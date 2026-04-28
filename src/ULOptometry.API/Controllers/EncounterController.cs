using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ULOptometry.API.Data;
using ULOptometry.API.DTOs.Encounter;
using ULOptometry.API.Services;
using ULOptometry.Domain.Entities;
using ULOptometry.Domain.Enums;

namespace ULOptometry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EncounterController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;

    public EncounterController(ApplicationDbContext db, INotificationService notifications, IAuditService audit)
    {
        _db = db;
        _notifications = notifications;
        _audit = audit;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role)!;

    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CreateEncounter([FromBody] CreateEncounterRequest request)
    {
        var booking = await _db.Bookings
            .Include(b => b.CubicleAssignment)
            .Include(b => b.Encounter)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.AcceptedByStudentId == CurrentUserId && b.Status == BookingStatus.Accepted);

        if (booking == null) return BadRequest(new { message = "Booking not found or not accepted by you" });
        if (booking.Encounter != null) return Conflict(new { message = "Encounter already exists for this booking" });

        if (request.EncounterType == EncounterType.Offsite)
        {
            if (string.IsNullOrWhiteSpace(request.OffsiteSupervisorName) || string.IsNullOrWhiteSpace(request.OffsiteOpNumber))
                return BadRequest(new { message = "Offsite encounters require supervisor name and OP number" });
        }

        var encounter = new Encounter
        {
            BookingId = request.BookingId,
            StudentId = CurrentUserId,
            SupervisorId = booking.CubicleAssignment?.SupervisorId,
            EncounterType = request.EncounterType,
            Status = EncounterStatus.Draft,
            PresentingComplaint = request.PresentingComplaint,
            OcularHistory = request.OcularHistory,
            MedicalHistory = request.MedicalHistory,
            VisualAcuityDistance = request.VisualAcuityDistance,
            VisualAcuityNear = request.VisualAcuityNear,
            Refraction = request.Refraction,
            AnteriorSegment = request.AnteriorSegment,
            PosteriorSegment = request.PosteriorSegment,
            IntraocularPressure = request.IntraocularPressure,
            AdditionalFindings = request.AdditionalFindings,
            Diagnosis = request.Diagnosis,
            ManagementPlan = request.ManagementPlan,
            Prescription = request.Prescription,
            Referral = request.Referral,
            ReflectionNotes = request.ReflectionNotes,
            LearningOutcomes = request.LearningOutcomes,
            OffsiteSupervisorName = request.OffsiteSupervisorName,
            OffsiteOpNumber = request.OffsiteOpNumber,
            ClinicalHours = request.ClinicalHours
        };

        _db.Encounters.Add(encounter);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "CreateEncounter", "Encounter", encounter.Id);

        return CreatedAtAction(nameof(GetEncounter), new { id = encounter.Id }, new { encounter.Id, encounter.Status });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UpdateEncounter(int id, [FromBody] CreateEncounterRequest request)
    {
        var encounter = await _db.Encounters.FindAsync(id);
        if (encounter == null || encounter.StudentId != CurrentUserId) return NotFound();
        if (encounter.IsLocked) return BadRequest(new { message = "Encounter is locked and cannot be edited" });
        if (encounter.Status == EncounterStatus.Submitted || encounter.Status == EncounterStatus.Approved)
            return BadRequest(new { message = "Cannot edit a submitted or approved encounter" });

        encounter.PresentingComplaint = request.PresentingComplaint;
        encounter.OcularHistory = request.OcularHistory;
        encounter.MedicalHistory = request.MedicalHistory;
        encounter.VisualAcuityDistance = request.VisualAcuityDistance;
        encounter.VisualAcuityNear = request.VisualAcuityNear;
        encounter.Refraction = request.Refraction;
        encounter.AnteriorSegment = request.AnteriorSegment;
        encounter.PosteriorSegment = request.PosteriorSegment;
        encounter.IntraocularPressure = request.IntraocularPressure;
        encounter.AdditionalFindings = request.AdditionalFindings;
        encounter.Diagnosis = request.Diagnosis;
        encounter.ManagementPlan = request.ManagementPlan;
        encounter.Prescription = request.Prescription;
        encounter.Referral = request.Referral;
        encounter.ReflectionNotes = request.ReflectionNotes;
        encounter.LearningOutcomes = request.LearningOutcomes;
        encounter.ClinicalHours = request.ClinicalHours;
        encounter.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "UpdateEncounter", "Encounter", id);

        return Ok(new { encounter.Id, encounter.Status });
    }

    [HttpPost("{id}/submit")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitEncounter(int id)
    {
        var encounter = await _db.Encounters
            .Include(e => e.Booking).ThenInclude(b => b.CubicleAssignment)
            .FirstOrDefaultAsync(e => e.Id == id && e.StudentId == CurrentUserId);

        if (encounter == null) return NotFound();
        if (encounter.IsLocked) return BadRequest(new { message = "Encounter is locked" });
        if (encounter.Status != EncounterStatus.Draft && encounter.Status != EncounterStatus.RequiresRevision)
            return BadRequest(new { message = "Encounter cannot be submitted in its current state" });

        if (encounter.EncounterType == EncounterType.Offsite)
        {
            encounter.Status = EncounterStatus.Approved;
            encounter.IsLocked = true;
            encounter.ApprovedAt = DateTime.UtcNow;
            encounter.SubmittedAt = DateTime.UtcNow;
            await AddPoERecord(encounter);
            await MaskPatientData(encounter.BookingId);
        }
        else
        {
            encounter.Status = EncounterStatus.Submitted;
            encounter.SubmittedAt = DateTime.UtcNow;

            if (encounter.SupervisorId.HasValue)
            {
                await _notifications.SendAsync(encounter.SupervisorId.Value, "Encounter Submitted",
                    "A student has submitted an encounter for your review.", "EncounterSubmitted",
                    $"/encounters/{encounter.Id}");
            }
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "SubmitEncounter", "Encounter", id);

        return Ok(new { encounter.Id, encounter.Status, encounter.IsLocked });
    }

    [HttpPost("{id}/review")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> ReviewEncounter(int id, [FromBody] ReviewEncounterRequest request)
    {
        var encounter = await _db.Encounters
            .Include(e => e.Booking)
            .FirstOrDefaultAsync(e => e.Id == id && e.Status == EncounterStatus.Submitted);

        if (encounter == null) return NotFound(new { message = "Encounter not found or not in submitted state" });

        if (encounter.SupervisorId != CurrentUserId)
            return Forbid();

        encounter.SupervisorComments = request.Comments;
        encounter.ReviewedAt = DateTime.UtcNow;

        if (request.Approve)
        {
            encounter.Status = EncounterStatus.Approved;
            encounter.IsLocked = true;
            encounter.ApprovedAt = DateTime.UtcNow;

            await AddPoERecord(encounter);
            await MaskPatientData(encounter.BookingId);

            await _notifications.SendAsync(encounter.StudentId, "Encounter Approved",
                "Your encounter has been approved and is now locked.", "EncounterApproved");

            var booking = await _db.Bookings.FindAsync(encounter.BookingId);
            if (booking != null)
            {
                booking.Status = BookingStatus.Completed;
                booking.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            encounter.Status = EncounterStatus.RequiresRevision;
            await _notifications.SendAsync(encounter.StudentId, "Encounter Requires Revision",
                $"Your encounter requires revision. Supervisor comments: {request.Comments}", "EncounterRevision");
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, request.Approve ? "ApproveEncounter" : "RequestRevision", "Encounter", id, after: new { request.Approve, request.Comments });

        return Ok(new { encounter.Id, encounter.Status });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEncounter(int id)
    {
        var encounter = await _db.Encounters
            .Include(e => e.Student)
            .Include(e => e.Supervisor)
            .Include(e => e.Booking).ThenInclude(b => b.PatientInfo).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (encounter == null) return NotFound();

        var role = CurrentRole;
        if (role == "Student" && encounter.StudentId != CurrentUserId) return Forbid();
        if (role == "Supervisor" && encounter.SupervisorId != CurrentUserId) return Forbid();
        if (role == "Patient")
        {
            var patientInfo = await _db.PatientInfos.FirstOrDefaultAsync(p => p.UserId == CurrentUserId);
            if (patientInfo == null || encounter.Booking.PatientInfoId != patientInfo.Id) return Forbid();
        }

        var patientName = encounter.Booking.PatientInfo.IsMasked
            ? "Patient data masked (POPIA)"
            : encounter.Booking.PatientInfo.User.FirstName + " " + encounter.Booking.PatientInfo.User.LastName;

        return Ok(new
        {
            encounter.Id,
            encounter.BookingId,
            PatientName = patientName,
            StudentName = encounter.Student.FirstName + " " + encounter.Student.LastName,
            SupervisorName = encounter.Supervisor != null ? encounter.Supervisor.FirstName + " " + encounter.Supervisor.LastName : null,
            encounter.EncounterType,
            encounter.Status,
            encounter.PresentingComplaint,
            encounter.OcularHistory,
            encounter.MedicalHistory,
            encounter.VisualAcuityDistance,
            encounter.VisualAcuityNear,
            encounter.Refraction,
            encounter.AnteriorSegment,
            encounter.PosteriorSegment,
            encounter.IntraocularPressure,
            encounter.AdditionalFindings,
            encounter.Diagnosis,
            encounter.ManagementPlan,
            encounter.Prescription,
            encounter.Referral,
            encounter.ReflectionNotes,
            encounter.LearningOutcomes,
            OffsiteSupervisorName = (role == "Patient") ? null : encounter.OffsiteSupervisorName,
            OffsiteOpNumber = (role == "Patient") ? null : encounter.OffsiteOpNumber,
            encounter.SupervisorComments,
            encounter.SubmittedAt,
            encounter.ApprovedAt,
            encounter.IsLocked,
            encounter.ClinicalHours
        });
    }

    private async Task AddPoERecord(Encounter encounter)
    {
        var booking = await _db.Bookings.FindAsync(encounter.BookingId);
        var clinicType = booking?.ClinicType ?? ClinicType.GeneralOptometry;
        var poe = new PoERecord
        {
            StudentId = encounter.StudentId,
            EncounterId = encounter.Id,
            ClinicType = clinicType,
            Hours = encounter.ClinicalHours,
            RecordedAt = DateTime.UtcNow
        };
        _db.PoERecords.Add(poe);
    }

    private async Task MaskPatientData(int bookingId)
    {
        var booking = await _db.Bookings.Include(b => b.PatientInfo).FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking?.PatientInfo != null && !booking.PatientInfo.IsMasked)
        {
            booking.PatientInfo.IsMasked = true;
            booking.PatientInfo.IdNumber = null;
            booking.PatientInfo.Address = null;
            booking.PatientInfo.UpdatedAt = DateTime.UtcNow;
        }
    }
}

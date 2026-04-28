using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ULOptometry.API.Data;
using ULOptometry.API.DTOs.Booking;
using ULOptometry.API.Services;
using ULOptometry.Domain.Enums;

namespace ULOptometry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class StudentController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;

    public StudentController(ApplicationDbContext db, INotificationService notifications, IAuditService audit)
    {
        _db = db;
        _notifications = notifications;
        _audit = audit;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = new
        {
            PendingBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending && b.CubicleAssignment != null && b.CubicleAssignment.StudentId == CurrentUserId),
            ActiveBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Accepted && b.AcceptedByStudentId == CurrentUserId),
            SubmittedEncounters = await _db.Encounters.CountAsync(e => e.StudentId == CurrentUserId && e.Status == EncounterStatus.Submitted),
            ApprovedEncounters = await _db.Encounters.CountAsync(e => e.StudentId == CurrentUserId && e.Status == EncounterStatus.Approved),
            TotalPoEHours = await _db.PoERecords.Where(p => p.StudentId == CurrentUserId).SumAsync(p => (double?)p.Hours) ?? 0
        };
        return Ok(stats);
    }

    [HttpGet("booking-queue")]
    public async Task<IActionResult> GetBookingQueue()
    {
        var assignments = await _db.CubicleAssignments
            .Where(ca => ca.StudentId == CurrentUserId)
            .Select(ca => ca.Id)
            .ToListAsync();

        var bookings = await _db.Bookings
            .Include(b => b.PatientInfo).ThenInclude(p => p.User)
            .Include(b => b.ClinicSession)
            .Include(b => b.CubicleAssignment).ThenInclude(ca => ca!.Cubicle)
            .Where(b => b.Status == BookingStatus.Pending && b.CubicleAssignmentId.HasValue && assignments.Contains(b.CubicleAssignmentId!.Value))
            .OrderBy(b => b.ClinicSession.SessionDate)
            .Select(b => new
            {
                b.Id,
                PatientName = b.PatientInfo.User.FirstName + " " + b.PatientInfo.User.LastName,
                b.ClinicType,
                b.Status,
                SessionDate = b.ClinicSession.SessionDate,
                SessionType = b.ClinicSession.SessionType,
                CubicleNumber = b.CubicleAssignment!.Cubicle.CubicleNumber,
                b.CreatedAt
            }).ToListAsync();

        return Ok(bookings);
    }

    [HttpPost("bookings/{id}/accept")]
    public async Task<IActionResult> AcceptBooking(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.CubicleAssignment)
            .Include(b => b.PatientInfo).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(b => b.Id == id && b.Status == BookingStatus.Pending);

        if (booking == null) return NotFound(new { message = "Booking not found or already processed" });

        if (booking.CubicleAssignment?.StudentId != CurrentUserId)
            return Forbid();

        booking.Status = BookingStatus.Accepted;
        booking.AcceptedByStudentId = CurrentUserId;
        booking.AcceptedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notifications.SendAsync(booking.PatientInfo.UserId, "Booking Accepted",
            $"Your appointment has been accepted by a student clinician.", "BookingAccepted");
        await _audit.LogAsync(CurrentUserId, "AcceptBooking", "Booking", id);

        return Ok(new { message = "Booking accepted" });
    }

    [HttpPost("bookings/{id}/cancel")]
    public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingRequest request)
    {
        var booking = await _db.Bookings
            .Include(b => b.PatientInfo).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(b => b.Id == id && b.AcceptedByStudentId == CurrentUserId && b.Status == BookingStatus.Accepted);

        if (booking == null) return NotFound(new { message = "Booking not found or cannot be cancelled" });

        booking.Status = BookingStatus.Pending;
        booking.AcceptedByStudentId = null;
        booking.AcceptedAt = null;
        booking.CancellationReason = request.Reason;
        await _db.SaveChangesAsync();

        await _notifications.SendAsync(booking.PatientInfo.UserId, "Booking Update",
            "Your appointment has been returned to the queue and will be reassigned.", "BookingCancelled");
        await _audit.LogAsync(CurrentUserId, "CancelBooking", "Booking", id, after: new { request.Reason });

        return Ok(new { message = "Booking cancelled and returned to queue" });
    }

    [HttpGet("encounters")]
    public async Task<IActionResult> GetMyEncounters()
    {
        var encounters = await _db.Encounters
            .Include(e => e.Booking).ThenInclude(b => b.PatientInfo).ThenInclude(p => p.User)
            .Include(e => e.Supervisor)
            .Where(e => e.StudentId == CurrentUserId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.BookingId,
                PatientName = e.Booking.PatientInfo.IsMasked ? "Masked" : e.Booking.PatientInfo.User.FirstName + " " + e.Booking.PatientInfo.User.LastName,
                e.EncounterType,
                e.Status,
                ClinicType = e.Booking.ClinicType,
                e.SubmittedAt,
                e.ApprovedAt,
                e.IsLocked,
                e.ClinicalHours,
                SupervisorName = e.Supervisor != null ? e.Supervisor.FirstName + " " + e.Supervisor.LastName : null
            }).ToListAsync();

        return Ok(encounters);
    }
}

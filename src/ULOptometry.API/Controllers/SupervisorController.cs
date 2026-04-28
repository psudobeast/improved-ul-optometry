using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ULOptometry.API.Data;
using ULOptometry.Domain.Enums;

namespace ULOptometry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Supervisor")]
public class SupervisorController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SupervisorController(ApplicationDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = new
        {
            PendingReview = await _db.Encounters.CountAsync(e => e.SupervisorId == CurrentUserId && e.Status == EncounterStatus.Submitted),
            ApprovedToday = await _db.Encounters.CountAsync(e => e.SupervisorId == CurrentUserId && e.Status == EncounterStatus.Approved && e.ApprovedAt.HasValue && e.ApprovedAt.Value.Date == DateTime.UtcNow.Date),
            AssignedCubicles = await _db.CubicleAssignments.CountAsync(ca => ca.SupervisorId == CurrentUserId && ca.ClinicSession.SessionDate.Date == DateTime.UtcNow.Date)
        };
        return Ok(stats);
    }

    [HttpGet("review-queue")]
    public async Task<IActionResult> GetReviewQueue()
    {
        var encounters = await _db.Encounters
            .Include(e => e.Student)
            .Include(e => e.Booking).ThenInclude(b => b.PatientInfo).ThenInclude(p => p.User)
            .Include(e => e.Booking).ThenInclude(b => b.CubicleAssignment).ThenInclude(ca => ca!.Cubicle)
            .Where(e => e.SupervisorId == CurrentUserId && e.Status == EncounterStatus.Submitted)
            .OrderBy(e => e.SubmittedAt)
            .Select(e => new
            {
                e.Id,
                e.BookingId,
                StudentName = e.Student.FirstName + " " + e.Student.LastName,
                PatientName = e.Booking.PatientInfo.IsMasked ? "Masked" : e.Booking.PatientInfo.User.FirstName + " " + e.Booking.PatientInfo.User.LastName,
                ClinicType = e.Booking.ClinicType,
                CubicleNumber = e.Booking.CubicleAssignment != null ? (int?)e.Booking.CubicleAssignment.Cubicle.CubicleNumber : null,
                e.EncounterType,
                e.Status,
                e.SubmittedAt
            }).ToListAsync();

        return Ok(encounters);
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> GetMyAssignments([FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow.Date;
        var assignments = await _db.CubicleAssignments
            .Include(ca => ca.Cubicle)
            .Include(ca => ca.Student)
            .Include(ca => ca.ClinicSession)
            .Where(ca => ca.SupervisorId == CurrentUserId && ca.ClinicSession.SessionDate.Date == targetDate.Date)
            .Select(ca => new
            {
                ca.Id,
                CubicleNumber = ca.Cubicle.CubicleNumber,
                StudentName = ca.Student.FirstName + " " + ca.Student.LastName,
                ca.ClinicSession.SessionDate,
                ca.ClinicSession.SessionType
            }).ToListAsync();

        return Ok(assignments);
    }

    [HttpGet("poe")]
    public async Task<IActionResult> GetPoE([FromQuery] int? studentId)
    {
        var query = _db.PoERecords
            .Include(p => p.Student)
            .Where(p => p.Student.Role == UserRole.Student);

        if (studentId.HasValue) query = query.Where(p => p.StudentId == studentId.Value);

        var records = await query
            .GroupBy(p => new { p.StudentId, p.Student.FirstName, p.Student.LastName })
            .Select(g => new
            {
                StudentId = g.Key.StudentId,
                StudentName = g.Key.FirstName + " " + g.Key.LastName,
                TotalCases = g.Count(),
                TotalHours = g.Sum(p => p.Hours)
            }).ToListAsync();

        return Ok(records);
    }
}

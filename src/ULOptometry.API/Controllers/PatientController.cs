using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ULOptometry.API.Data;
using ULOptometry.API.DTOs.Booking;
using ULOptometry.API.Services;
using ULOptometry.Domain.Entities;
using ULOptometry.Domain.Enums;

namespace ULOptometry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Patient")]
public class PatientController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;

    public PatientController(ApplicationDbContext db, INotificationService notifications, IAuditService audit)
    {
        _db = db;
        _notifications = notifications;
        _audit = audit;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("bookings")]
    public async Task<IActionResult> GetMyBookings()
    {
        var patientInfo = await _db.PatientInfos.FirstOrDefaultAsync(p => p.UserId == CurrentUserId);
        if (patientInfo == null) return NotFound(new { message = "Patient profile not found" });

        var bookings = await _db.Bookings
            .Include(b => b.ClinicSession)
            .Where(b => b.PatientInfoId == patientInfo.Id)
            .OrderByDescending(b => b.ClinicSession.SessionDate)
            .Select(b => new
            {
                b.Id,
                b.ClinicType,
                b.Status,
                SessionDate = b.ClinicSession.SessionDate,
                SessionType = b.ClinicSession.SessionType,
                b.CreatedAt
            }).ToListAsync();

        return Ok(bookings);
    }

    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var patientInfo = await _db.PatientInfos.FirstOrDefaultAsync(p => p.UserId == CurrentUserId);
        if (patientInfo == null) return NotFound(new { message = "Patient profile not found" });

        var session = await _db.ClinicSessions.FindAsync(request.ClinicSessionId);
        if (session == null || !session.IsActive) return BadRequest(new { message = "Session not available" });

        var booking = new Booking
        {
            PatientInfoId = patientInfo.Id,
            ClinicSessionId = request.ClinicSessionId,
            ClinicType = request.ClinicType,
            Status = BookingStatus.Pending
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        await _notifications.SendAsync(CurrentUserId, "Booking Confirmed",
            $"Your {request.ClinicType} appointment has been booked for {session.SessionDate:d}.", "BookingCreated");
        await _audit.LogAsync(CurrentUserId, "CreateBooking", "Booking", booking.Id, after: request);

        return CreatedAtAction(nameof(GetMyBookings), new { id = booking.Id }, new { booking.Id, booking.Status });
    }

    [HttpGet("bookings/{id}")]
    public async Task<IActionResult> GetBooking(int id)
    {
        var patientInfo = await _db.PatientInfos.FirstOrDefaultAsync(p => p.UserId == CurrentUserId);
        if (patientInfo == null) return NotFound();

        var booking = await _db.Bookings
            .Include(b => b.ClinicSession)
            .Include(b => b.Encounter)
            .Where(b => b.Id == id && b.PatientInfoId == patientInfo.Id)
            .Select(b => new
            {
                b.Id,
                b.ClinicType,
                b.Status,
                SessionDate = b.ClinicSession.SessionDate,
                SessionType = b.ClinicSession.SessionType,
                Encounter = b.Encounter == null ? null : new
                {
                    b.Encounter.Id,
                    b.Encounter.Status,
                    b.Encounter.Diagnosis,
                    b.Encounter.ManagementPlan,
                    b.Encounter.Prescription
                }
            }).FirstOrDefaultAsync();

        if (booking == null) return NotFound();
        return Ok(booking);
    }

    [HttpGet("available-sessions")]
    public async Task<IActionResult> GetAvailableSessions([FromQuery] DateTime? date)
    {
        var query = _db.ClinicSessions.Where(s => s.IsActive && s.SessionDate >= DateTime.UtcNow.Date).AsQueryable();
        if (date.HasValue) query = query.Where(s => s.SessionDate.Date == date.Value.Date);

        var sessions = await query
            .OrderBy(s => s.SessionDate).ThenBy(s => s.SessionType)
            .Select(s => new { s.Id, s.SessionDate, s.SessionType, s.StartTime, s.EndTime })
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var notifications = await _db.Notifications
            .Where(n => n.UserId == CurrentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return Ok(notifications);
    }

    [HttpPut("notifications/{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == CurrentUserId);
        if (n == null) return NotFound();
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }
}

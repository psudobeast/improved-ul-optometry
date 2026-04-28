using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ULOptometry.API.Data;
using ULOptometry.API.DTOs.Admin;
using ULOptometry.API.Services;
using ULOptometry.Domain.Entities;
using ULOptometry.Domain.Enums;

namespace ULOptometry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AuthService _authService;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;

    public AdminController(ApplicationDbContext db, AuthService authService, INotificationService notifications, IAuditService audit)
    {
        _db = db;
        _authService = authService;
        _notifications = notifications;
        _audit = audit;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var today = DateTime.UtcNow.Date;
        var stats = new
        {
            TotalUsers = await _db.Users.CountAsync(),
            ActiveSessions = await _db.ClinicSessions.CountAsync(s => s.SessionDate.Date == today && s.IsActive),
            PendingBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
            PendingEncounters = await _db.Encounters.CountAsync(e => e.Status == EncounterStatus.Submitted),
            TotalEncountersToday = await _db.Encounters.CountAsync(e => e.CreatedAt.Date == today),
            UnreadNotifications = await _db.Notifications.CountAsync(n => !n.IsRead && n.UserId == CurrentUserId)
        };
        return Ok(stats);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] UserRole? role)
    {
        var query = _db.Users.AsQueryable();
        if (role.HasValue) query = query.Where(u => u.Role == role.Value);
        var users = await query.Select(u => new { u.Id, u.Username, u.Email, u.FirstName, u.LastName, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt }).ToListAsync();
        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { message = "Email already in use" });

        var tempPassword = $"ULOpt@{Guid.NewGuid().ToString()[..8]}";
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = _authService.HashPassword(tempPassword),
            RequiresPasswordChange = true,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (request.Role == UserRole.Patient)
        {
            _db.PatientInfos.Add(new PatientInfo { UserId = user.Id });
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(CurrentUserId, "CreateUser", "User", user.Id, after: new { user.Email, user.Role });

        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new { user.Id, user.Email, TemporaryPassword = tempPassword });
    }

    [HttpPut("users/{id}/activate")]
    public async Task<IActionResult> ActivateUser(int id, [FromQuery] bool active)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = active;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, active ? "ActivateUser" : "DeactivateUser", "User", id);
        return Ok(new { message = $"User {(active ? "activated" : "deactivated")}" });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "DeleteUser", "User", id);
        return NoContent();
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions([FromQuery] DateTime? date)
    {
        var query = _db.ClinicSessions.Include(s => s.CubicleAssignments).ThenInclude(ca => ca.Cubicle).AsQueryable();
        if (date.HasValue) query = query.Where(s => s.SessionDate.Date == date.Value.Date);
        var sessions = await query.OrderByDescending(s => s.SessionDate).ToListAsync();
        return Ok(sessions);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        var session = new ClinicSession
        {
            SessionDate = request.SessionDate,
            SessionType = request.SessionType,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };
        _db.ClinicSessions.Add(session);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "CreateSession", "ClinicSession", session.Id, after: request);
        return CreatedAtAction(nameof(GetSessions), new { id = session.Id }, session);
    }

    [HttpGet("cubicles")]
    public async Task<IActionResult> GetCubicles()
    {
        var cubicles = await _db.Cubicles.OrderBy(c => c.CubicleNumber).ToListAsync();
        return Ok(cubicles);
    }

    [HttpPost("cubicles/assign")]
    public async Task<IActionResult> AssignCubicle([FromBody] AssignCubicleRequest request)
    {
        var existing = await _db.CubicleAssignments.FirstOrDefaultAsync(ca =>
            ca.ClinicSessionId == request.ClinicSessionId && ca.CubicleId == request.CubicleId);
        if (existing != null) return Conflict(new { message = "Cubicle already assigned for this session" });

        var assignment = new CubicleAssignment
        {
            ClinicSessionId = request.ClinicSessionId,
            CubicleId = request.CubicleId,
            StudentId = request.StudentId,
            SupervisorId = request.SupervisorId
        };
        _db.CubicleAssignments.Add(assignment);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "AssignCubicle", "CubicleAssignment", assignment.Id, after: request);
        return Ok(assignment);
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetAllBookings([FromQuery] BookingStatus? status, [FromQuery] DateTime? date)
    {
        var query = _db.Bookings
            .Include(b => b.PatientInfo).ThenInclude(p => p.User)
            .Include(b => b.ClinicSession)
            .Include(b => b.AcceptedByStudent)
            .AsQueryable();

        if (status.HasValue) query = query.Where(b => b.Status == status.Value);
        if (date.HasValue) query = query.Where(b => b.ClinicSession.SessionDate.Date == date.Value.Date);

        var bookings = await query.OrderByDescending(b => b.CreatedAt).Select(b => new
        {
            b.Id,
            PatientName = b.PatientInfo.User.FirstName + " " + b.PatientInfo.User.LastName,
            b.ClinicType,
            b.Status,
            SessionDate = b.ClinicSession.SessionDate,
            SessionType = b.ClinicSession.SessionType,
            StudentName = b.AcceptedByStudent != null ? b.AcceptedByStudent.FirstName + " " + b.AcceptedByStudent.LastName : null,
            b.CreatedAt
        }).ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("reports/poe-summary")]
    public async Task<IActionResult> GetPoESummary([FromQuery] int? studentId)
    {
        var query = _db.PoERecords.Include(p => p.Student).AsQueryable();
        if (studentId.HasValue) query = query.Where(p => p.StudentId == studentId.Value);

        var summary = await query.GroupBy(p => new { p.StudentId, p.Student.FirstName, p.Student.LastName, p.ClinicType })
            .Select(g => new
            {
                StudentId = g.Key.StudentId,
                StudentName = g.Key.FirstName + " " + g.Key.LastName,
                ClinicType = g.Key.ClinicType.ToString(),
                TotalCases = g.Count(),
                TotalHours = g.Sum(p => p.Hours)
            }).ToListAsync();

        return Ok(summary);
    }

    [HttpGet("reports/audit")]
    public async Task<IActionResult> GetAuditLog([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? userId)
    {
        var query = _db.AuditLogs.Include(a => a.User).AsQueryable();
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value);
        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);

        var logs = await query.OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                UserEmail = a.User != null ? a.User.Email : "System",
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Before,
                a.After,
                a.IpAddress,
                a.CreatedAt
            }).Take(1000).ToListAsync();

        return Ok(logs);
    }

    [HttpPost("notifications/broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest request)
    {
        await _notifications.BroadcastAsync(request.Title, request.Message, request.NotificationType);
        await _audit.LogAsync(CurrentUserId, "BroadcastNotification", "Notification", null, after: request);
        return Ok(new { message = "Broadcast sent" });
    }
}

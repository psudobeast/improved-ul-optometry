using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ULOptometry.API.Data;

namespace ULOptometry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student,Admin")]
public class PoEController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PoEController(ApplicationDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet]
    public async Task<IActionResult> GetPoESummary([FromQuery] int? studentId)
    {
        var targetStudentId = CurrentRole == "Admin" ? studentId : CurrentUserId;

        var records = await _db.PoERecords
            .Include(p => p.Encounter)
            .Where(p => targetStudentId == null || p.StudentId == targetStudentId)
            .ToListAsync();

        var summary = new
        {
            TotalCases = records.Count,
            TotalHours = records.Sum(r => r.Hours),
            ByClinicType = records.GroupBy(r => r.ClinicType)
                .Select(g => new
                {
                    ClinicType = g.Key.ToString(),
                    Cases = g.Count(),
                    Hours = g.Sum(r => r.Hours)
                }).ToList()
        };

        return Ok(summary);
    }

    [HttpGet("records")]
    public async Task<IActionResult> GetPoERecords([FromQuery] int? studentId)
    {
        var targetStudentId = CurrentRole == "Admin" ? studentId : CurrentUserId;

        var records = await _db.PoERecords
            .Include(p => p.Encounter).ThenInclude(e => e.Booking)
            .Where(p => targetStudentId == null || p.StudentId == targetStudentId)
            .OrderByDescending(p => p.RecordedAt)
            .Select(p => new
            {
                p.Id,
                p.StudentId,
                p.EncounterId,
                p.ClinicType,
                p.Hours,
                p.RecordedAt,
                ClinicTypeName = p.ClinicType.ToString()
            }).ToListAsync();

        return Ok(records);
    }
}

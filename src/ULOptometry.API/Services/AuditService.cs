using System.Text.Json;
using ULOptometry.API.Data;
using ULOptometry.Domain.Entities;

namespace ULOptometry.API.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(int? userId, string action, string entityType, int? entityId, object? before = null, object? after = null, string? ipAddress = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = before != null ? JsonSerializer.Serialize(before) : null,
            After = after != null ? JsonSerializer.Serialize(after) : null,
            IpAddress = ipAddress
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}

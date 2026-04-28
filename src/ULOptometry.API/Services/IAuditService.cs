namespace ULOptometry.API.Services;
public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entityType, int? entityId, object? before = null, object? after = null, string? ipAddress = null);
}

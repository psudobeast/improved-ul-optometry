namespace ULOptometry.API.Services;
public interface INotificationService
{
    Task SendAsync(int userId, string title, string message, string? notificationType = null, string? actionUrl = null);
    Task BroadcastAsync(string title, string message, string? notificationType = null);
}

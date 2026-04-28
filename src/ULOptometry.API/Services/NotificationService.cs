using ULOptometry.API.Data;
using ULOptometry.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ULOptometry.API.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task SendAsync(int userId, string title, string message, string? notificationType = null, string? actionUrl = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            NotificationType = notificationType,
            ActionUrl = actionUrl
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }

    public async Task BroadcastAsync(string title, string message, string? notificationType = null)
    {
        var users = await _db.Users.Where(u => u.IsActive && !u.IsDeleted).ToListAsync();
        var notifications = users.Select(u => new Notification
        {
            UserId = u.Id,
            Title = title,
            Message = message,
            NotificationType = notificationType
        });
        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync();
    }
}

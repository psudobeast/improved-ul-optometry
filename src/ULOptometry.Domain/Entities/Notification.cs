namespace ULOptometry.Domain.Entities;
public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public string? ActionUrl { get; set; }
    public string? NotificationType { get; set; }
}

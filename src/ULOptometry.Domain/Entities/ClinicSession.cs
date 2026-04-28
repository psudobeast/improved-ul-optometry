using ULOptometry.Domain.Enums;
namespace ULOptometry.Domain.Entities;
public class ClinicSession : BaseEntity
{
    public DateTime SessionDate { get; set; }
    public SessionType SessionType { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<CubicleAssignment> CubicleAssignments { get; set; } = new List<CubicleAssignment>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

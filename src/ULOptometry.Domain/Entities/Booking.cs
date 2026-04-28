using ULOptometry.Domain.Enums;
namespace ULOptometry.Domain.Entities;
public class Booking : BaseEntity
{
    public int PatientInfoId { get; set; }
    public PatientInfo PatientInfo { get; set; } = null!;
    public int ClinicSessionId { get; set; }
    public ClinicSession ClinicSession { get; set; } = null!;
    public int? CubicleAssignmentId { get; set; }
    public CubicleAssignment? CubicleAssignment { get; set; }
    public ClinicType ClinicType { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public int? AcceptedByStudentId { get; set; }
    public User? AcceptedByStudent { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? CancellationReason { get; set; }
    public Encounter? Encounter { get; set; }
}

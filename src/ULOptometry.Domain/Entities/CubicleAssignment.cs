namespace ULOptometry.Domain.Entities;
public class CubicleAssignment : BaseEntity
{
    public int ClinicSessionId { get; set; }
    public ClinicSession ClinicSession { get; set; } = null!;
    public int CubicleId { get; set; }
    public Cubicle Cubicle { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int SupervisorId { get; set; }
    public User Supervisor { get; set; } = null!;
}

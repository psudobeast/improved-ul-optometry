using ULOptometry.Domain.Enums;
namespace ULOptometry.Domain.Entities;
public class PoERecord : BaseEntity
{
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int EncounterId { get; set; }
    public Encounter Encounter { get; set; } = null!;
    public ClinicType ClinicType { get; set; }
    public double Hours { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

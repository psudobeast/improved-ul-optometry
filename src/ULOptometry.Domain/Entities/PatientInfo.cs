namespace ULOptometry.Domain.Entities;
public class PatientInfo : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? IdNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? MedicalAid { get; set; }
    public bool IsMasked { get; set; } = false;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

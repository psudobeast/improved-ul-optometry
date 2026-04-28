using ULOptometry.Domain.Enums;
namespace ULOptometry.Domain.Entities;
public class Encounter : BaseEntity
{
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int? SupervisorId { get; set; }
    public User? Supervisor { get; set; }
    public EncounterType EncounterType { get; set; }
    public EncounterStatus Status { get; set; } = EncounterStatus.Draft;
    public string? PresentingComplaint { get; set; }
    public string? OcularHistory { get; set; }
    public string? MedicalHistory { get; set; }
    public string? VisualAcuityDistance { get; set; }
    public string? VisualAcuityNear { get; set; }
    public string? Refraction { get; set; }
    public string? AnteriorSegment { get; set; }
    public string? PosteriorSegment { get; set; }
    public string? IntraocularPressure { get; set; }
    public string? AdditionalFindings { get; set; }
    public string? Diagnosis { get; set; }
    public string? ManagementPlan { get; set; }
    public string? Prescription { get; set; }
    public string? Referral { get; set; }
    public string? ReflectionNotes { get; set; }
    public string? LearningOutcomes { get; set; }
    public string? OffsiteSupervisorName { get; set; }
    public string? OffsiteOpNumber { get; set; }
    public string? SupervisorComments { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsLocked { get; set; } = false;
    public double ClinicalHours { get; set; } = 1.0;
    public PoERecord? PoERecord { get; set; }
}

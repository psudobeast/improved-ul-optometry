using ULOptometry.Domain.Enums;
namespace ULOptometry.API.DTOs.Encounter;
public record EncounterDto(
    int Id, int BookingId, int StudentId, string StudentName,
    int? SupervisorId, string? SupervisorName,
    EncounterType EncounterType, EncounterStatus Status,
    string? PresentingComplaint, string? OcularHistory, string? MedicalHistory,
    string? VisualAcuityDistance, string? VisualAcuityNear, string? Refraction,
    string? AnteriorSegment, string? PosteriorSegment, string? IntraocularPressure, string? AdditionalFindings,
    string? Diagnosis, string? ManagementPlan, string? Prescription, string? Referral,
    string? ReflectionNotes, string? LearningOutcomes,
    string? OffsiteSupervisorName, string? OffsiteOpNumber,
    string? SupervisorComments, DateTime? SubmittedAt, DateTime? ApprovedAt,
    bool IsLocked, double ClinicalHours);

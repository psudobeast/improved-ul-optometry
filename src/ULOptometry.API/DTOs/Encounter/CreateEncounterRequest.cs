using ULOptometry.Domain.Enums;
namespace ULOptometry.API.DTOs.Encounter;
public record CreateEncounterRequest(
    int BookingId, EncounterType EncounterType,
    string? PresentingComplaint, string? OcularHistory, string? MedicalHistory,
    string? VisualAcuityDistance, string? VisualAcuityNear, string? Refraction,
    string? AnteriorSegment, string? PosteriorSegment, string? IntraocularPressure, string? AdditionalFindings,
    string? Diagnosis, string? ManagementPlan, string? Prescription, string? Referral,
    string? ReflectionNotes, string? LearningOutcomes,
    string? OffsiteSupervisorName, string? OffsiteOpNumber,
    double ClinicalHours = 1.0);

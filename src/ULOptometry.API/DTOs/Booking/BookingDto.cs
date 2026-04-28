using ULOptometry.Domain.Enums;
namespace ULOptometry.API.DTOs.Booking;
public record BookingDto(int Id, int PatientInfoId, string PatientName, int ClinicSessionId, DateTime SessionDate, SessionType SessionType, ClinicType ClinicType, BookingStatus Status, int? AcceptedByStudentId, string? StudentName, DateTime CreatedAt);

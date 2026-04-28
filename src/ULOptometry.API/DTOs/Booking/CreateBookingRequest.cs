using ULOptometry.Domain.Enums;
namespace ULOptometry.API.DTOs.Booking;
public record CreateBookingRequest(int ClinicSessionId, ClinicType ClinicType);

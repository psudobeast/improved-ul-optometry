using ULOptometry.Domain.Enums;
namespace ULOptometry.API.DTOs.Admin;
public record CreateSessionRequest(DateTime SessionDate, SessionType SessionType, TimeSpan StartTime, TimeSpan EndTime);

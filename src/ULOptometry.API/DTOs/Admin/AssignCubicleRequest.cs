namespace ULOptometry.API.DTOs.Admin;
public record AssignCubicleRequest(int ClinicSessionId, int CubicleId, int StudentId, int SupervisorId);

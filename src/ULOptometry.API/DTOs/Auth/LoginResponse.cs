namespace ULOptometry.API.DTOs.Auth;
public record LoginResponse(string Token, string Role, bool RequiresPasswordChange, int UserId, string FullName);

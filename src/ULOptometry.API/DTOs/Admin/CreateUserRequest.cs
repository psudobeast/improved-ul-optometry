using ULOptometry.Domain.Enums;
namespace ULOptometry.API.DTOs.Admin;
public record CreateUserRequest(string Username, string Email, string FirstName, string LastName, UserRole Role, string? PhoneNumber);

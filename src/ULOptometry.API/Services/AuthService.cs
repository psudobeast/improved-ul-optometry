using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ULOptometry.API.Data;
using ULOptometry.API.DTOs.Auth;
using ULOptometry.Domain.Entities;

namespace ULOptometry.API.Services;

public class AuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditService _audit;

    public AuthService(ApplicationDbContext db, IConfiguration config, IAuditService audit)
    {
        _db = db;
        _config = config;
        _audit = audit;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = GenerateToken(user);
        await _audit.LogAsync(user.Id, "Login", "User", user.Id, ipAddress: ipAddress);

        return new LoginResponse(token, user.Role.ToString(), user.RequiresPasswordChange, user.Id, $"{user.FirstName} {user.LastName}");
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.RequiresPasswordChange = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "PasswordChanged", "User", userId);
        return true;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
}

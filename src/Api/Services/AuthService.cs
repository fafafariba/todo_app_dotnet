using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Data;
using Api.DTOs.Auth;
using Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing is not null)
            throw new InvalidOperationException("EMAIL_TAKEN");

        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        Console.WriteLine("[auth] User registered", new { userId = user.Id });

        var token = GenerateToken(user.Id);
        return new AuthResponse(token, new AuthUserResponse(user.Id, user.Email, user.Name));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            throw new InvalidOperationException("INVALID_CREDENTIALS");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            throw new InvalidOperationException("INVALID_CREDENTIALS");

        var token = GenerateToken(user.Id);
        return new AuthResponse(token, new AuthUserResponse(user.Id, user.Email, user.Name));
    }

    private string GenerateToken(string userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] { new Claim("userId", userId) };

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddDays(1),
            claims: claims,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

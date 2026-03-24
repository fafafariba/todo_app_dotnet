namespace Api.DTOs.Auth;

public record RegisterRequest(string Email, string? Name, string Password);

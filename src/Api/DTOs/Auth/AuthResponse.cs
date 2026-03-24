namespace Api.DTOs.Auth;

public record AuthResponse(string Token, AuthUserResponse User);

public record AuthUserResponse(string Id, string Email, string? Name);

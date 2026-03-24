using Api.DTOs.Auth;
using Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors });

        try
        {
            var result = await _authService.RegisterAsync(request);
            return Created("", new { data = result });
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_TAKEN")
        {
            return Conflict(new { error = "Email already in use" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors });

        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(new { data = result });
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_CREDENTIALS")
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }
    }
}

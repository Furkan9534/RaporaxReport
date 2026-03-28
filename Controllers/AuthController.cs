using AuthApi.DTOs;
using AuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [EnableRateLimiting("StrictAuth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(response);
    }

    [EnableRateLimiting("ModerateAuth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        
        if (response.Success)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [EnableRateLimiting("StrictAuth")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var response = await _authService.ForgotPasswordAsync(request);
        return Ok(response);
    }

    [EnableRateLimiting("ModerateAuth")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var response = await _authService.ResetPasswordAsync(request);
        
        if (response.Success)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [Authorize]
    [EnableRateLimiting("ModerateAuth")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var response = await _authService.ChangePasswordAsync(userId, request);

        if (response.Success)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value 
                 ?? User.FindFirst(ClaimTypes.Email)?.Value;

        var companyId = User.FindFirst("companyId")?.Value;

        var roles = User.FindAll(ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList();

        if (userId == null || email == null || companyId == null)
        {
            return Unauthorized();
        }

        var response = new CurrentUserResponse
        {
            UserId = userId,
            Email = email,
            CompanyId = companyId,
            Roles = roles
        };

        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "You have successfully accessed an Admin-only protected endpoint." });
    }
}

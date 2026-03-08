using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaporaxReport.API.Data;
using RaporaxReport.API.DTOs;
using RaporaxReport.API.Models;
using RaporaxReport.API.Services;

namespace RaporaxReport.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        TokenService tokenService,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _db = db;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        // OWASP A07: Kullanıcı varlığını açıklamıyoruz
        if (user == null || !user.IsActive)
            return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });

        if (!user.Company.IsActive)
            return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return StatusCode(429, new { message = "Hesap kilitlendi. Lütfen 15 dakika sonra tekrar deneyin." });

        if (!result.Succeeded)
            return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = await _tokenService.CreateTokenAsync(user);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                CompanyId = user.CompanyId,
                Role = roles.FirstOrDefault() ?? string.Empty
            }
        });
    }
}

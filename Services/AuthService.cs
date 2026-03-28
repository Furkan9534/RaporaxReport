using AuthApi.Data;
using AuthApi.DTOs;
using AuthApi.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _dbContext;
    private readonly IEmailSender _emailSender;

    public AuthService(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        AppDbContext dbContext,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _emailSender = emailSender;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return null;
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return _tokenService.GenerateToken(user, roles);
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new RegisterResponse { Success = false, Message = "Registration failed. Please verify your details." };
        }

        await using var transaction = await _dbContext.BeginTransactionAsync();
        try
        {
            var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.Name == request.CompanyName);
            if (company == null)
            {
                company = new Company { Name = request.CompanyName };
                _dbContext.Companies.Add(company);
                await _dbContext.SaveChangesAsync();
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                CompanyId = company.Id
            };

            var userResult = await _userManager.CreateAsync(user, request.Password);
            if (!userResult.Succeeded)
            {
                await transaction.RollbackAsync();

                var safeErrors = userResult.Errors
                    .Where(e => e.Code.StartsWith("Password"))
                    .Select(e => e.Description)
                    .ToList();

                return new RegisterResponse 
                { 
                    Success = false, 
                    Message = "Registration failed.", 
                    Errors = safeErrors.Any() ? safeErrors : null 
                };
            }

            await transaction.CommitAsync();
            return new RegisterResponse { Success = true, Message = "Registration successful." };
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return new RegisterResponse { Success = false, Message = "Registration failed. Please verify your details." };
        }
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new ChangePasswordResponse { Success = false, Message = "Password change failed. Please verify your details." };
        }

        var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!isCurrentPasswordValid)
        {
            return new ChangePasswordResponse { Success = false, Message = "Current password is incorrect." };
        }

        var isSamePassword = await _userManager.CheckPasswordAsync(user, request.NewPassword);
        if (isSamePassword)
        {
            return new ChangePasswordResponse { Success = false, Message = "New password cannot be the same as the current password." };
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        
        if (!result.Succeeded)
        {
            var safeErrors = result.Errors
                .Where(e => e.Code.StartsWith("Password"))
                .Select(e => e.Description)
                .ToList();

            return new ChangePasswordResponse 
            { 
                Success = false, 
                Message = "Password change failed.", 
                Errors = safeErrors.Any() ? safeErrors : null 
            };
        }

        return new ChangePasswordResponse { Success = true, Message = "Password changed successfully." };
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
             var token = await _userManager.GeneratePasswordResetTokenAsync(user);
             await _emailSender.SendEmailAsync(request.Email, "Reset Password", $"Your password reset token is:\n{token}\n\nDo not share this token.");
        }

        return new ForgotPasswordResponse 
        { 
            Success = true, 
            Message = "If an account exists with that email, a password reset link has been sent." 
        };
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new ResetPasswordResponse { Success = false, Message = "Password reset failed. Invalid request." };
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        
        if (!result.Succeeded)
        {
            var safeErrors = result.Errors
                .Where(e => e.Code.StartsWith("Password"))
                .Select(e => e.Description)
                .ToList();

            return new ResetPasswordResponse 
            { 
                Success = false, 
                Message = "Password reset failed.", 
                Errors = safeErrors.Any() ? safeErrors : null 
            };
        }

        return new ResetPasswordResponse { Success = true, Message = "Password has been reset successfully." };
    }
}

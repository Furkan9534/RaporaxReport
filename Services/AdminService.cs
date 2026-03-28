using AuthApi.DTOs;
using AuthApi.Entities;
using Microsoft.AspNetCore.Identity;

namespace AuthApi.Services;

public class AdminService : IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<AssignRoleResponse> AssignRoleAsync(AssignRoleRequest request)
    {
        var validRoles = new[] { "Admin", "ReportViewer", "ReportCreator" };
        var roleExists = await _roleManager.RoleExistsAsync(request.Role);

        if (!roleExists || !validRoles.Contains(request.Role))
        {
            return new AssignRoleResponse { Success = false, Message = "Assignment failed. Invalid role or user details." };
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AssignRoleResponse { Success = false, Message = "Assignment failed. Invalid role or user details." };
        }

        var alreadyHasRole = await _userManager.IsInRoleAsync(user, request.Role);
        if (alreadyHasRole)
        {
            return new AssignRoleResponse { Success = false, Message = "User already has the specified role." };
        }

        var result = await _userManager.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded)
        {
            return new AssignRoleResponse { Success = false, Message = "Assignment failed. Please try again." };
        }

        return new AssignRoleResponse { Success = true, Message = "Role assigned successfully." };
    }
}

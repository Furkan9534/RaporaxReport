using AuthApi.DTOs;

namespace AuthApi.Services;

public interface IAdminService
{
    Task<AssignRoleResponse> AssignRoleAsync(AssignRoleRequest request);
}

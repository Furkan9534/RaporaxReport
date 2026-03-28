using AuthApi.DTOs;
using AuthApi.Entities;

namespace AuthApi.Services;

public interface ITokenService
{
    LoginResponse GenerateToken(ApplicationUser user, IList<string> roles);
}

using Microsoft.AspNetCore.Identity;

namespace AuthApi.Entities;

public class ApplicationUser : IdentityUser
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
}

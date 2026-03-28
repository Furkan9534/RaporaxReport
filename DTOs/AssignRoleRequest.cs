using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs;

public class AssignRoleRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Role { get; set; }
}

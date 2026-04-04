using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    public required string Password { get; set; }

    [Required]
    [MinLength(2, ErrorMessage = "Company name is too short.")]
    public required string CompanyName { get; set; }
}

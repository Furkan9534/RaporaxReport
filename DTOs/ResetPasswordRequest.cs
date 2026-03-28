using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs;

public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Token { get; set; }

    [Required]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters long.")]
    public required string NewPassword { get; set; }
}

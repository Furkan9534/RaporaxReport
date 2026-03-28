using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs;

public class ChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters long.")]
    public required string NewPassword { get; set; }
}

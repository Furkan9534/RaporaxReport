using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}

namespace AuthApi.DTOs;

public class ResetPasswordResponse
{
    public required bool Success { get; set; }
    public required string Message { get; set; }
    public List<string>? Errors { get; set; }
}

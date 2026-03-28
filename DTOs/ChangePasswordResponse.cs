namespace AuthApi.DTOs;

public class ChangePasswordResponse
{
    public required bool Success { get; set; }
    public required string Message { get; set; }
    public List<string>? Errors { get; set; }
}

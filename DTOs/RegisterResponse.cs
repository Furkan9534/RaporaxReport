namespace AuthApi.DTOs;

public class RegisterResponse
{
    public required bool Success { get; set; }
    public required string Message { get; set; }
    public List<string>? Errors { get; set; }
}

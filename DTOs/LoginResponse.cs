namespace AuthApi.DTOs;

public class LoginResponse
{
    public required string AccessToken { get; set; }
    public required string TokenType { get; set; } = "Bearer";
    public required DateTime ExpiresAtUtc { get; set; }
}

namespace AuthApi.DTOs;

public class CurrentUserResponse
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public required string CompanyId { get; set; }
    public required List<string> Roles { get; set; }
}

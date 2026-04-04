using AuthApi.Entities;
using AuthApi.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;

namespace AuthApi.Tests.Services;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        var jwtSettingsMock = new Mock<IConfigurationSection>();
        
        jwtSettingsMock.Setup(s => s["Key"]).Returns("supersecret_very_long_test_key_1234567890!");
        jwtSettingsMock.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSettingsMock.Setup(s => s["Audience"]).Returns("TestAudience");
        jwtSettingsMock.Setup(s => s["DurationInMinutes"]).Returns("60");

        _configMock.Setup(c => c.GetSection("JwtSettings")).Returns(jwtSettingsMock.Object);

        _tokenService = new TokenService(_configMock.Object);
    }

    [Fact]
    public void GenerateToken_IncludesRequiredClaims_WhenUserHasRoles()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", Email = "test@test.com", CompanyId = Guid.NewGuid() };
        var roles = new List<string> { "Admin", "ReportViewer" };

        // Act
        var result = _tokenService.GenerateToken(user, roles);

        // Assert
        Assert.NotNull(result);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.AccessToken);

        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "user-123");
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@test.com");
        Assert.Contains(jwtToken.Claims, c => c.Type == "companyId" && c.Value == user.CompanyId.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == "role" && c.Value == "Admin");
        Assert.Contains(jwtToken.Claims, c => c.Type == "role" && c.Value == "ReportViewer");
    }

    [Fact]
    public void GenerateToken_WorksSafely_WhenRolesEmpty()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", Email = "test@test.com", CompanyId = Guid.NewGuid() };
        var roles = new List<string>();

        // Act
        var result = _tokenService.GenerateToken(user, roles);

        // Assert
        Assert.NotNull(result);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.AccessToken);
        
        Assert.DoesNotContain(jwtToken.Claims, c => c.Type == "role");
    }

    [Fact]
    public void GenerateToken_IncludesIssuerAudienceAndExpirationCorrectly()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", Email = "test@test.com", CompanyId = Guid.NewGuid() };
        
        // Act
        var result = _tokenService.GenerateToken(user, new List<string>());

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.AccessToken);

        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Equal("TestAudience", jwtToken.Audiences.First());
        
        // Check Expiration is roughly 60 mins from now
        var exp = result.ExpiresAtUtc;
        Assert.True(exp > DateTime.UtcNow.AddMinutes(59) && exp < DateTime.UtcNow.AddMinutes(61));
    }
}

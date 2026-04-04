using AuthApi.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuthApi.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    
    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_InvalidModel_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { Email = "not-an-email" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_SuccessfulRegistration_AndDuplicateFailsSafely()
    {
        var email = $"test_{Guid.NewGuid()}@test.com";
        var req = new RegisterRequest { Email = email, Password = "Password123!", CompanyName = "Acme" };
        
        // 1. Success
        var response1 = await _client.PostAsJsonAsync("/api/auth/register", req);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var res1 = await response1.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.True(res1!.Success);

        // 2. Duplicate Safe Failure
        var response2 = await _client.PostAsJsonAsync("/api/auth/register", req);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
        var res2 = await response2.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.False(res2!.Success);
        Assert.Equal("Registration failed. Please verify your details.", res2.Message);
    }

    [Fact]
    public async Task Login_InvalidModel_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "not-an-email" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var req = new LoginRequest { Email = "nobody@nowhere.com", Password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var email = $"login_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "Password123!", CompanyName = "Acme" });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var res = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(res!.AccessToken);
    }

    [Fact]
    public async Task Me_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ValidToken_ReturnsUserInfo()
    {
        var email = $"me_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "Password123!", CompanyName = "Acme" });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);
        
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var meData = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Equal(email, meData!.Email);
        Assert.NotNull(meData.CompanyId);
    }

    [Fact]
    public async Task ChangePassword_WorksAndFailsSafely()
    {
        var email = $"cp_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "Password123!", CompanyName = "Acme" });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        // Fail safely
        var wrongReq = new ChangePasswordRequest { CurrentPassword = "wrong", NewPassword = "NewPassword123!" };
        var wrongRes = await _client.PostAsJsonAsync("/api/auth/change-password", wrongReq);
        Assert.Equal(HttpStatusCode.BadRequest, wrongRes.StatusCode);
        
        // Success
        var rightReq = new ChangePasswordRequest { CurrentPassword = "Password123!", NewPassword = "NewPassword123!" };
        var rightRes = await _client.PostAsJsonAsync("/api/auth/change-password", rightReq);
        Assert.Equal(HttpStatusCode.OK, rightRes.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_TokenContainsCompanyIdAndEmail()
    {
        var email = $"claims_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "Password123!", CompanyName = "ClaimsTestCo" });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(loginData!.AccessToken);

        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        Assert.Contains(jwt.Claims, c => c.Type == "companyId" && !string.IsNullOrEmpty(c.Value));
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && !string.IsNullOrEmpty(c.Value));
    }

    [Fact]
    public async Task ForgotPassword_ReturnsGenericSuccess_ForUnknownEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest { Email = "unknown@test.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var res = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
        Assert.True(res!.Success);
    }

    [Fact]
    public async Task ResetPassword_FailsSafely_WithInvalidToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest { Email = "test@test.com", Token = "invalid", NewPassword = "Password123!" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var res = await response.Content.ReadFromJsonAsync<ResetPasswordResponse>();
        Assert.False(res!.Success);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = $"weak_{Guid.NewGuid()}@test.com",
            Password = "weak",
            CompanyName = "Any"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

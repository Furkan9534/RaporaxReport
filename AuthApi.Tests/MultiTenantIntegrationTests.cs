using AuthApi.DTOs;
using System.Net.Http.Json;

namespace AuthApi.Tests.Integration;

public class MultiTenantIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MultiTenantIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<CurrentUserResponse> RegisterLoginAndGetMeAsync(string email, string companyName)
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password123!",
            CompanyName = companyName
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password123!"
        });

        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        var meResponse = await _client.SendAsync(request);
        return (await meResponse.Content.ReadFromJsonAsync<CurrentUserResponse>())!;
    }

    [Fact]
    public async Task DifferentCompanyUsers_HaveDifferentCompanyIds()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await RegisterLoginAndGetMeAsync($"usera_{id}@test.com", $"CompanyA_{id}");
        var userB = await RegisterLoginAndGetMeAsync($"userb_{id}@test.com", $"CompanyB_{id}");

        Assert.NotEqual(userA.CompanyId, userB.CompanyId);
    }

    [Fact]
    public async Task SameCompanyUsers_HaveSameCompanyId()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var companyName = $"SharedCompany_{id}";

        var userA = await RegisterLoginAndGetMeAsync($"shared_a_{id}@test.com", companyName);
        var userB = await RegisterLoginAndGetMeAsync($"shared_b_{id}@test.com", companyName);

        Assert.Equal(userA.CompanyId, userB.CompanyId);
    }

    [Fact]
    public async Task JwtToken_ContainsCorrectCompanyId_ForEachTenant()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await RegisterLoginAndGetMeAsync($"jwt_a_{id}@test.com", $"TenantA_{id}");
        var userB = await RegisterLoginAndGetMeAsync($"jwt_b_{id}@test.com", $"TenantB_{id}");

        Assert.NotNull(userA.CompanyId);
        Assert.NotNull(userB.CompanyId);
        Assert.NotEqual(userA.CompanyId, userB.CompanyId);
    }
}

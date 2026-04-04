using AuthApi.DTOs;
using AuthApi.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuthApi.Tests.Integration;

public class RoleProtectionIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public RoleProtectionIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndLoginAsync(string email, string password = "Password123!")
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = password, CompanyName = "TestCo" });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginData!.AccessToken;
    }

    private async Task AssignRoleAsync(string email, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        await userManager.AddToRoleAsync(user!, role);
    }

    // --- ReportViewer ---

    [Fact]
    public async Task ReportViewer_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/test/report-viewer");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReportViewer_WithoutRole_Returns403()
    {
        var email = $"norole_{Guid.NewGuid()}@test.com";
        var token = await RegisterAndLoginAsync(email);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/report-viewer");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReportViewer_WithReportViewerRole_Returns200()
    {
        var email = $"viewer_{Guid.NewGuid()}@test.com";
        await RegisterAndLoginAsync(email);
        await AssignRoleAsync(email, "ReportViewer");

        // Login again to get token with the new role
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/report-viewer");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReportViewer_WithReportCreatorRole_Returns403()
    {
        var email = $"creator_on_viewer_{Guid.NewGuid()}@test.com";
        await RegisterAndLoginAsync(email);
        await AssignRoleAsync(email, "ReportCreator");

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/report-viewer");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- ReportCreator ---

    [Fact]
    public async Task ReportCreator_NoToken_Returns401()
    {
        var response = await _client.PostAsync("/api/test/report-creator", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReportCreator_WithoutRole_Returns403()
    {
        var email = $"norole2_{Guid.NewGuid()}@test.com";
        var token = await RegisterAndLoginAsync(email);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/test/report-creator");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReportCreator_WithReportCreatorRole_Returns200()
    {
        var email = $"creator_{Guid.NewGuid()}@test.com";
        await RegisterAndLoginAsync(email);
        await AssignRoleAsync(email, "ReportCreator");

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/test/report-creator");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReportCreator_WithAdminRole_Returns403()
    {
        var email = $"admin_on_creator_{Guid.NewGuid()}@test.com";
        await RegisterAndLoginAsync(email);
        await AssignRoleAsync(email, "Admin");

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/test/report-creator");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

using AuthApi.DTOs;
using AuthApi.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuthApi.Tests.Integration;

public class AdminIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AdminIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AssignRole_NoToken_GetsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/roles/assign", new AssignRoleRequest { Email = "test@test.com", Role = "ReportViewer" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AssignRole_NonAdmin_GetsForbidden()
    {
        var email = $"nonadmin_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "Password123!", CompanyName = "Acme" });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/roles/assign");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);
        request.Content = JsonContent.Create(new AssignRoleRequest { Email = email, Role = "ReportViewer" });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignRole_Admin_CanAssignRole()
    {
        var adminEmail = $"admin_{Guid.NewGuid()}@test.com";
        var userEmail = $"user_{Guid.NewGuid()}@test.com";

        // Register both users
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = adminEmail, Password = "Password123!", CompanyName = "Acme" });
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = userEmail, Password = "Password123!", CompanyName = "Acme" });

        // Make admin user an Admin via DB bypass
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            await userManager.AddToRoleAsync(adminUser!, "Admin");
        }

        // Login as Admin
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = adminEmail, Password = "Password123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Assign Role
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/roles/assign");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);
        request.Content = JsonContent.Create(new AssignRoleRequest { Email = userEmail, Role = "ReportViewer" });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaporaxReport.API.Models;

namespace RaporaxReport.API.Data;

public static class SeedData
{
    public const string RoleAdmin = "Admin";
    public const string RoleReportCreator = "ReportCreator";
    public const string RoleReportViewer = "ReportViewer";

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await db.Database.MigrateAsync();

        // Roller
        foreach (var role in new[] { RoleAdmin, RoleReportCreator, RoleReportViewer })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Firma
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company == null)
        {
            company = new Company { Name = "Raporax Demo Firma" };
            db.Companies.Add(company);
            await db.SaveChangesAsync();
        }

        // Admin kullanıcı
        const string adminEmail = "admin@raporax.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Sistem Admin",
                CompanyId = company.Id,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, RoleAdmin);
        }
    }
}

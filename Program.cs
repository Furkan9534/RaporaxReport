using System.Text;
using AuthApi.Data;
using AuthApi.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    options.User.RequireUniqueEmail = true;
    
    // Stateless API doesn't use cookies by default, but we ensure it here
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var keyString = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
var key = Encoding.ASCII.GetBytes(keyString);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Proda gerçek SSL gerekebilir.
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 4. Role-based Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ReportViewerOnly", policy => policy.RequireRole("ReportViewer"));
    options.AddPolicy("ReportCreatorOnly", policy => policy.RequireRole("ReportCreator"));
});


// Add services to the container.
builder.Services.AddTransient<AuthApi.Services.IEmailSender, AuthApi.Services.FakeEmailSender>();
builder.Services.AddScoped<AuthApi.Services.ITokenService, AuthApi.Services.TokenService>();
builder.Services.AddScoped<AuthApi.Services.IAuthService, AuthApi.Services.AuthService>();
builder.Services.AddScoped<AuthApi.Services.IAdminService, AuthApi.Services.AdminService>();
builder.Services.AddControllers(); // Standard API controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT token girin. Örnek: Bearer eyJhbGci..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var isTesting = builder.Environment.EnvironmentName == "Testing";

if (!isTesting)
{
    // 5. Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync("{\"success\":false,\"message\":\"Too many requests. Please try again later.\"}", cancellationToken: token);
        };

        options.AddFixedWindowLimiter("StrictAuth", opt =>
        {
            opt.Window = TimeSpan.FromSeconds(30);
            opt.PermitLimit = 3;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        options.AddFixedWindowLimiter("ModerateAuth", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 10;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (!isTesting)
{
    app.UseRateLimiter();
}

// 6. Middleware Pipeline Order
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var roles = new[] { "Admin", "ReportViewer", "ReportCreator" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    if (!isTesting)
    {
        var adminEmail = config["AdminSettings:Email"]
            ?? throw new InvalidOperationException("AdminSettings:Email is not configured.");
        var adminPassword = config["AdminSettings:Password"]
            ?? throw new InvalidOperationException("AdminSettings:Password is not configured.");

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var systemCompany = await db.Companies.FirstOrDefaultAsync(c => c.Name == "System");
            if (systemCompany == null)
            {
                systemCompany = new Company { Name = "System" };
                db.Companies.Add(systemCompany);
                await db.SaveChangesAsync();
            }

            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                CompanyId = systemCompany.Id
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create admin user: {errors}");
            }
        }
    }
}

app.Run();

public partial class Program { }

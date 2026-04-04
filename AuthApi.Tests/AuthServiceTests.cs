using AuthApi.Data;
using AuthApi.DTOs;
using AuthApi.Entities;
using AuthApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace AuthApi.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

        _tokenServiceMock = new Mock<ITokenService>();
        _emailSenderMock = new Mock<IEmailSender>();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        var dbContext = new TestAppDbContext(options);

        _authService = new AuthService(
            _userManagerMock.Object, 
            _roleManagerMock.Object, 
            _tokenServiceMock.Object, 
            dbContext, 
            _emailSenderMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_ForUnknownUser()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
        var result = await _authService.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "123" });
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_ForWrongPassword()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);
        var result = await _authService.LoginAsync(new LoginRequest { Email = user.Email, Password = "wrong" });
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenResponse_ForValidCredentials()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "valid")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _tokenServiceMock.Setup(x => x.GenerateToken(user, It.IsAny<IList<string>>()))
            .Returns(new LoginResponse { AccessToken = "token", TokenType = "Bearer", ExpiresAtUtc = DateTime.UtcNow });
        
        var result = await _authService.LoginAsync(new LoginRequest { Email = user.Email, Password = "valid" });
        Assert.NotNull(result);
        Assert.Equal("token", result.AccessToken);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsFailure_ForDuplicateEmail()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        var result = await _authService.RegisterAsync(new RegisterRequest { Email = user.Email, Password = "123", CompanyName = "Any" });
        Assert.False(result.Success);
        Assert.Equal("Registration failed. Please verify your details.", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsSafePasswordErrors_ForWeakPassword()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
        var identityErrors = new[] { new IdentityError { Code = "PasswordTooShort", Description = "Too short" } };
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var result = await _authService.RegisterAsync(new RegisterRequest { Email = "test@test.com", Password = "weak", CompanyName = "Any" });

        Assert.False(result.Success);
        Assert.NotNull(result.Errors);
        Assert.Contains("Too short", result.Errors);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFailure_ForWrongCurrentPassword()
    {
        var user = new ApplicationUser { Id = "123" };
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);
        
        var result = await _authService.ChangePasswordAsync(user.Id, new ChangePasswordRequest { CurrentPassword = "wrong", NewPassword = "new" });
        
        Assert.False(result.Success);
        Assert.Equal("Current password is incorrect.", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFailure_WhenNewPasswordIsSame()
    {
        var user = new ApplicationUser { Id = "123" };
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "pass")).ReturnsAsync(true);
        
        var result = await _authService.ChangePasswordAsync(user.Id, new ChangePasswordRequest { CurrentPassword = "pass", NewPassword = "pass" });
        
        Assert.False(result.Success);
        Assert.Equal("New password cannot be the same as the current password.", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsSuccess_ForValidPasswordChange()
    {
        var user = new ApplicationUser { Id = "123" };
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "old")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "new")).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "old", "new")).ReturnsAsync(IdentityResult.Success);
        
        var result = await _authService.ChangePasswordAsync(user.Id, new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "new" });
        
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsGenericSuccess_ForUnknownEmail()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
        
        var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "unknown@test.com" });
        
        Assert.True(result.Success);
        Assert.Contains("If an account exists", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsGenericFailure_ForInvalidToken()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
        
        var result = await _authService.ResetPasswordAsync(new ResetPasswordRequest { Email = "test@test.com", Token = "invalid", NewPassword = "new" });
        
        Assert.False(result.Success);
        Assert.Equal("Password reset failed. Invalid request.", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsSafePasswordErrors_ForWeakPassword()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var identityErrors = new[] { new IdentityError { Code = "PasswordTooShort", Description = "Too short" } };
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "token", "weak"))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var result = await _authService.ResetPasswordAsync(new ResetPasswordRequest { Email = user.Email, Token = "token", NewPassword = "weak" });

        Assert.False(result.Success);
        Assert.NotNull(result.Errors);
        Assert.Contains("Too short", result.Errors);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsSuccess_ForValidToken()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "valid-token", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
            Token = "valid-token",
            NewPassword = "NewPassword123!"
        });

        Assert.True(result.Success);
        Assert.Equal("Password has been reset successfully.", result.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SendsEmail_WhenUserExists()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

        await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });

        _emailSenderMock.Verify(
            x => x.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_DoesNotSendEmail_WhenUserDoesNotExist()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

        await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "ghost@test.com" });

        _emailSenderMock.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}

public class TestAppDbContext : AppDbContext
{
    public TestAppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public override Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Mock<IDbContextTransaction>().Object);
    }
}

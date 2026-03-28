using AuthApi.DTOs;
using AuthApi.Entities;
using AuthApi.Services;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AuthApi.Tests.Services;

public class AdminServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly AdminService _adminService;

    public AdminServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

        _adminService = new AdminService(_userManagerMock.Object, _roleManagerMock.Object);
    }

    [Fact]
    public async Task AssignRoleAsync_FailsSafely_ForUnknownUser()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

        var result = await _adminService.AssignRoleAsync(new AssignRoleRequest { Email = "test@test.com", Role = "Admin" });

        Assert.False(result.Success);
        Assert.Equal("Assignment failed. Invalid role or user details.", result.Message);
    }

    [Fact]
    public async Task AssignRoleAsync_FailsSafely_ForInvalidRole()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _roleManagerMock.Setup(x => x.RoleExistsAsync("FakeRole")).ReturnsAsync(false);

        var result = await _adminService.AssignRoleAsync(new AssignRoleRequest { Email = user.Email, Role = "FakeRole" });

        Assert.False(result.Success);
        Assert.Equal("Assignment failed. Invalid role or user details.", result.Message);
    }

    [Fact]
    public async Task AssignRoleAsync_ReturnsClearResponse_IfUserAlreadyHasRole()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        var role = "Admin";

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _roleManagerMock.Setup(x => x.RoleExistsAsync(role)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.IsInRoleAsync(user, role)).ReturnsAsync(true);

        var result = await _adminService.AssignRoleAsync(new AssignRoleRequest { Email = user.Email, Role = role });

        Assert.False(result.Success);
        Assert.Equal("User already has the specified role.", result.Message);
    }

    [Fact]
    public async Task AssignRoleAsync_Succeeds_ForValidUserAndRole()
    {
        var user = new ApplicationUser { Email = "test@test.com" };
        var role = "Admin";

        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _roleManagerMock.Setup(x => x.RoleExistsAsync(role)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.IsInRoleAsync(user, role)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.AddToRoleAsync(user, role)).ReturnsAsync(IdentityResult.Success);

        var result = await _adminService.AssignRoleAsync(new AssignRoleRequest { Email = user.Email, Role = role });

        Assert.True(result.Success);
        Assert.Equal("Role assigned successfully.", result.Message);
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.User.Controllers;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;

namespace SeguesTests.UnitTests.Users;

public class UserControllerTests
{
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly Mock<RoleManager<Role>> _mockRoleManager;
    private readonly Mock<IUserService> _mockUserService;
    private readonly UserController _controller;
    private const string TestUserId = "pedro-77";

    public UserControllerTests()
    {
        _mockUserManager = MockHelper.MockUserManager(new List<AppUser>());
        _mockRoleManager = MockHelper.MockRoleManager<Role>();
        _mockUserService = new Mock<IUserService>();
        
        _controller = new UserController(_mockUserManager.Object, _mockRoleManager.Object, _mockUserService.Object);
        MockHelper.SetupControllerContext(_controller); // TestUserId is default in this helper
    }

    [Fact]
    public async Task Index_UserNotFound_ReturnsChallengeResult()
    {
        _mockUserService.Setup(s => s.GetUserForEditAsync(TestUserId))
            .ReturnsAsync((AppUser) null!);

        var result = await _controller.Index();

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Index_UserFound_ReturnsViewWithViewModel()
    {
        var user = MockHelper.CreateValidStudent();
        user.Id = TestUserId;
        user.FirstName = "Pedro";

        _mockUserService.Setup(s => s.GetUserForEditAsync(TestUserId))
            .ReturnsAsync(user);

        _mockUserManager.Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Client" });

        _mockRoleManager.Setup(r => r.FindByNameAsync("Client"))
            .ReturnsAsync(MockHelper.CreateValidRole());

        _mockUserService.Setup(s => s.GetSchoolsAsync())
            .ReturnsAsync([]);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<EditUserViewModel>(viewResult.Model);
        Assert.Equal("Pedro", model.FirstName);
    }

    [Fact]
    public async Task UpdateProfile_UserNotFound_ReturnsChallengeResult()
    {
        var model = MockHelper.CreateValidEditUserViewModel();
        _mockUserService.Setup(s => s.GetUserForEditAsync(TestUserId))
            .ReturnsAsync((AppUser) null!);

        var result = await _controller.UpdateProfile(model);

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_InvalidModelState_ReturnsViewWithModel()
    {
        var model = MockHelper.CreateValidEditUserViewModel();
        _controller.ModelState.AddModelError("Email", "Required");

        var user = MockHelper.CreateValidAppUser();
        user.Id = TestUserId;

        _mockUserService.Setup(s => s.GetUserForEditAsync(TestUserId))
            .ReturnsAsync(user);

        _mockUserService.Setup(s => s.GetSchoolsAsync())
            .ReturnsAsync([]);

        _mockUserManager.Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Client" });

        _mockRoleManager.Setup(r => r.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(MockHelper.CreateValidRole());

        var result = await _controller.UpdateProfile(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(_controller.Index), viewResult.ViewName);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task UpdateProfile_ValidModelState_Success_RedirectsToIndex()
    {
        var model = MockHelper.CreateValidEditUserViewModel();
        var user = MockHelper.CreateValidAppUser();
        user.Id = TestUserId;

        _mockUserService.Setup(s => s.GetUserForEditAsync(TestUserId))
            .ReturnsAsync(user);

        _mockUserService.Setup(s => s.UpdateUserProfileAsync(user, model))
            .ReturnsAsync(new ServiceResult(true, "Sucesso"));

        var result = await _controller.UpdateProfile(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(_controller.Index), redirectResult.ActionName);
    }

    [Fact]
    public async Task UpdateProfile_ValidModelState_Failure_ReturnsViewWithModel()
    {
        var model = MockHelper.CreateValidEditUserViewModel();
        var user = MockHelper.CreateValidAppUser();
        user.Id = TestUserId;

        _mockUserService.Setup(s => s.GetUserForEditAsync(TestUserId))
            .ReturnsAsync(user);

        _mockUserService.Setup(s => s.UpdateUserProfileAsync(user, model))
            .ReturnsAsync(new ServiceResult(false, "Erro"));

        _mockUserService.Setup(s => s.GetSchoolsAsync())
            .ReturnsAsync([]);

        var result = await _controller.UpdateProfile(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(_controller.Index), viewResult.ViewName);
        Assert.Equal(model, viewResult.Model);
    }
}
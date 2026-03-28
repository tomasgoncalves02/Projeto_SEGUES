using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.User.Controllers;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Security.Claims;
using MockQueryable.Moq;
using Xunit;

namespace Tests.UnitTests.User;

public class UserControllerTests
{
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly Mock<RoleManager<Role>> _mockRoleManager;
    private readonly Mock<IUserService> _mockUserService;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<AppUser>>();
        _mockUserManager = new Mock<UserManager<AppUser>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<Role>>();
        _mockRoleManager = new Mock<RoleManager<Role>>(roleStoreMock.Object, null!, null!, null!, null!);

        _mockUserService = new Mock<IUserService>();

        _controller = new UserController(_mockUserManager.Object, _mockRoleManager.Object, _mockUserService.Object);

        var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "Pedro")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsUser }
        };

        _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task Index_UserNotFound_ReturnsChallengeResult()
    {
        var usersList = new List<AppUser>().AsQueryable().BuildMock();
        _mockUserManager.Setup(u => u.Users).Returns(usersList);

        var result = await _controller.Index();

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Index_UserFound_ReturnsViewWithViewModel()
    {
        var user = MockHelper.CreateValidStudent();
        var usersList = new List<AppUser> { user }.AsQueryable().BuildMock();


        _mockUserManager.Setup(u => u.Users).Returns(usersList);
        _mockUserManager.Setup(u => u.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string> { "Client" });
        _mockRoleManager.Setup(r => r.FindByNameAsync("Client")).ReturnsAsync(MockHelper.CreateValidRole());
        _mockUserService.Setup(s => s.GetSchoolsAsync()).ReturnsAsync(new List<SelectListItem>());

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<EditUserViewModel>(viewResult.Model);
        Assert.Equal("Pedro", model.FirstName);
        Assert.Equal("12345", model.StudentNumber);
    }

    [Fact]
    public async Task UpdateProfile_UserNotFound_ReturnsChallengeResult()
    {
        var model = MockHelper.CreateValidEditUserViewModel();
        var usersList = new List<AppUser>().AsQueryable().BuildMock();
        _mockUserManager.Setup(u => u.Users).Returns(usersList);

        var result = await _controller.UpdateProfile(model);

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_InvalidModelState_ReturnsViewWithModel()
    {
        var model = MockHelper.CreateValidEditUserViewModel();
        _controller.ModelState.AddModelError("Email", "Required");

        var user = MockHelper.CreateValidAppUser();
        var usersList = new List<AppUser> { user }.AsQueryable().BuildMock();

        _mockUserManager.Setup(u => u.Users).Returns(usersList);
        _mockUserService.Setup(s => s.GetSchoolsAsync()).ReturnsAsync(new List<SelectListItem>());
        _mockUserManager.Setup(u => u.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string> { "Client" });
        _mockRoleManager.Setup(r => r.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(MockHelper.CreateValidRole());

        var result = await _controller.UpdateProfile(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task UpdateProfile_ValidModelState_Success_RedirectsToIndex()
    {
        var model = MockHelper.CreateValidEditUserViewModel();
        var user = MockHelper.CreateValidAppUser();
        var usersList = new List<AppUser> { user }.AsQueryable().BuildMock();

        _mockUserManager.Setup(u => u.Users).Returns(usersList);
        _mockUserService.Setup(s => s.UpdateUserProfileAsync(It.IsAny<AppUser>(), model))
     .ReturnsAsync(new ServiceResult(true, "Perfil atualizado com sucesso."));

        var result = await _controller.UpdateProfile(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task UpdateProfile_ValidModelState_Failure_ReturnsViewWithModel()
    {
        var model = MockHelper.CreateValidEditUserViewModel();
        var user = MockHelper.CreateValidAppUser();
        var usersList = new List<AppUser> { user }.AsQueryable().BuildMock();

        _mockUserManager.Setup(u => u.Users).Returns(usersList);
        _mockUserService.Setup(s => s.UpdateUserProfileAsync(It.IsAny<AppUser>(), model))
     .ReturnsAsync(new ServiceResult(false, "Erro ao atualizar perfil."));
        _mockUserService.Setup(s => s.GetSchoolsAsync()).ReturnsAsync(new List<SelectListItem>());

        var result = await _controller.UpdateProfile(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal(model, viewResult.Model);
    }
}
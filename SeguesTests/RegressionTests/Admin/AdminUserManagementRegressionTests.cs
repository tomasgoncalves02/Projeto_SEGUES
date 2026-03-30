using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;

namespace SeguesTests.RegressionTests.Admin;

public class AdminUserManagementRegressionTests
{
    [Fact]
    public async Task Deactivate_Self_ShouldBeBlockedAndShowError()
    {
        var storeMock = new Mock<IUserStore<AppUser>>();
        var userManagerMock = new Mock<UserManager<AppUser>>(storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var pedro = new AppUser
        {
            Id = "77",
            FirstName = "Pedro",
            LastName = "Teste",
            Email = "pedro@admin.pt",
            UserName = "pedro@admin.pt",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = new UserCategory { Name = "Admin" }
        };
        userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(pedro);

        var controller = new AdminUserManagementController(
            userManagerMock.Object,
            Mock.Of<IAdminService>(),
            Mock.Of<IUserService>(),
            Mock.Of<ILogger<AdminUserManagementController>>(),
            Mock.Of<IPdfService>());

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "pedro@admin.pt")], "mock");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var tempDataMock = new Mock<ITempDataDictionary>();
        controller.TempData = tempDataMock.Object;

        var result = await controller.Deactivate("1");

        Assert.IsType<RedirectToActionResult>(result);

        userManagerMock.Verify(m => m.SetLockoutEndDateAsync(It.IsAny<AppUser>(), It.IsAny<DateTimeOffset?>()), Times.Never);
        userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<AppUser>()), Times.Never);

        tempDataMock.VerifySet(t => t[It.Is<string>(k => k.Contains("Swal"))] = It.IsAny<object>(), Times.Once);
    }

    [Fact]
    public async Task Edit_ServiceFailure_ShouldMaintainStateAndRepopulateDropdowns()
    {
        var storeMock = new Mock<IUserStore<AppUser>>();
        var userManagerMock = new Mock<UserManager<AppUser>>(storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var adminServiceMock = new Mock<IAdminService>();
        var userServiceMock = new Mock<IUserService>();

        var pedro = new AppUser
        {
            Id = "77",
            FirstName = "Pedro",
            LastName = "Teste",
            Email = "pedro@test.pt",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = new UserCategory { Name = "Estudante" }
        };
        userServiceMock.Setup(s => s.GetUserForEditAsync("77")).ReturnsAsync(pedro);

        adminServiceMock.Setup(s => s.UpdateUserAdminAsync(It.IsAny<AppUser>(), It.IsAny<EditUserAdminViewModel>(), It.IsAny<IUrlHelper>(), It.IsAny<string>()))
            .ReturnsAsync(ServiceResult.Fail("Erro"));

        var controller = new AdminUserManagementController(
            userManagerMock.Object,
            adminServiceMock.Object,
            userServiceMock.Object,
            Mock.Of<ILogger<AdminUserManagementController>>(),
            Mock.Of<IPdfService>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http"; 

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var urlHelperMock = new Mock<IUrlHelper>();
        controller.Url = urlHelperMock.Object;

        var tempDataMock = new Mock<ITempDataDictionary>();
        controller.TempData = tempDataMock.Object;

        var model = new EditUserAdminViewModel
        {
            Id = "77",
            FirstName = "Pedro",
            LastName = "Teste",
            Email = "pedro@test.pt",
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            BirthDate = new DateTime(1990, 1, 1),
            Role = "Client",
            Category = "Estudante"
        };

        var result = await controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<EditUserAdminViewModel>(viewResult.Model);

        adminServiceMock.Verify(s => s.GetAllRolesForDropdownAsync(), Times.Once);
        adminServiceMock.Verify(s => s.GetAllCategoriesForDropdownAsync(), Times.Once);
        userServiceMock.Verify(s => s.GetSchoolsAsync(), Times.Once);
        tempDataMock.VerifySet(t => t[It.Is<string>(k => k.Contains("Swal"))] = It.IsAny<object>(), Times.Once);
    }
}
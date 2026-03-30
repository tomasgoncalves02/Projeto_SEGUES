using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using System.Reflection;

namespace SeguesTests.SecurityTests.Admin;

public class AdminCreateInternalAccountSecurityTests
{
    [Fact]
    public void Controller_ShouldBeRestrictedToAdminRole()
    {
        var type = typeof(AdminCreateInternalAccountController);
        var attribute = type.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("Admin", attribute.Roles);
    }

    [Fact]
    public void Controller_ShouldBelongToAdminArea()
    {
        var type = typeof(AdminCreateInternalAccountController);
        var attribute = type.GetCustomAttribute<AreaAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("Admin", attribute.RouteValue);
    }

    [Fact]
    public void CreateMethod_ShouldValidateAntiForgeryToken()
    {
        var method = typeof(AdminCreateInternalAccountController)
            .GetMethod("Create", [typeof(CreateInternalUserViewModel)]);

        var attribute = method?.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();

        Assert.NotNull(attribute);
    }

    [Fact]
    public async Task Create_ShouldCallService_OnlyWhenPedroIsAuthorized()
    {
        var adminServiceMock = new Mock<IAdminService>();
        var controller = new AdminCreateInternalAccountController(adminServiceMock.Object);

        var tempDataMock = new Mock<ITempDataDictionary>();
        controller.TempData = tempDataMock.Object;

        var pedroModel = new CreateInternalUserViewModel
        {
            FirstName = "Pedro",
            LastName = "Admin",
            Email = "pedro@segues.pt",
            AccountType = "Admin",
            Gender = Gender.Male,
            BirthDate = DateTime.Now.AddYears(-30)
        };

        adminServiceMock.Setup(s => s.CreateInternalUserAsync(pedroModel))
            .ReturnsAsync(ServiceResult.Ok("Sucesso"));

        var result = await controller.Create(pedroModel);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }
}
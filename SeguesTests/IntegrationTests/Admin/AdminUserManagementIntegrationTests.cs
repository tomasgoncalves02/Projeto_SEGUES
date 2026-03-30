using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using Xunit;

namespace SeguesTests.IntegrationTests.Admin;

public class AdminUserManagementIntegrationTests
{
    [Fact]
    public async Task Details_MapsUserToDtoCorrectly()
    {
        var (context, _, _) = MockHelper.GetIdentitySetup();

        var category = new UserCategory { Name = "Estudante" };
        var pedro = new AppUser
        {
            Id = "77",
            FirstName = "Pedro",
            LastName = "Integração",
            Email = "pedro@teste.pt",
            UserName = "pedro@teste.pt",
            BirthDate = new DateTime(1995, 5, 5),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = category,
            Balance = 50.5m,
            Status = Projeto_SEGUES.Models.Enums.UserStatus.Active,
            CreationDate = new DateTime(2023, 1, 1)
        };

        context.UserCategory.Add(category);
        context.Users.Add(pedro);
        await context.SaveChangesAsync();

        var storeMock = new Mock<IUserStore<AppUser>>();
        var userManagerMock = new Mock<UserManager<AppUser>>(storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.Users).Returns(context.Users);

        userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string> { "Client" });

        var adminServiceMock = new Mock<IAdminService>();
        var controller = new AdminUserManagementController(
            userManagerMock.Object, 
            adminServiceMock.Object,
            Mock.Of<IUserService>(),
            Mock.Of<ILogger<AdminUserManagementController>>(),
            Mock.Of<IPdfService>());

        var result = await controller.Details("77");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UserDto>(viewResult.Model);

        Assert.Equal("Pedro Integração", model.FullName);
        Assert.Equal("Estudante", model.CategoryName);
        Assert.True(model.IsActive);
        Assert.Contains("50,50", model.BalanceFormatted);
    }

    [Fact]
    public async Task Activate_UpdatesDatabaseStatusAndClearsLockout()
    {
        var storeMock = new Mock<IUserStore<AppUser>>();
        var userManagerMock = new Mock<UserManager<AppUser>>(storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var category = new UserCategory { Name = "Estudante" };
        var pedro = new AppUser
        {
            Id = "88",
            FirstName = "Pedro",
            LastName = "Bloqueado",
            Email = "pedro.block@teste.pt",
            UserName = "pedro.block@teste.pt",
            BirthDate = new DateTime(1995, 5, 5),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = category,
            Status = Projeto_SEGUES.Models.Enums.UserStatus.Inactive,
            LockoutEnd = DateTimeOffset.MaxValue
        };

        userManagerMock.Setup(m => m.FindByIdAsync("88")).ReturnsAsync(pedro);
        userManagerMock.Setup(m => m.SetLockoutEndDateAsync(pedro, null))
            .Callback<AppUser, DateTimeOffset?>((u, d) => u.LockoutEnd = d)
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.UpdateAsync(pedro))
            .ReturnsAsync(IdentityResult.Success);

        var controller = new AdminUserManagementController(
            userManagerMock.Object,
            Mock.Of<IAdminService>(),
            Mock.Of<IUserService>(),
            Mock.Of<ILogger<AdminUserManagementController>>(),
            Mock.Of<IPdfService>());

        var tempDataMock = new Mock<ITempDataDictionary>();
        controller.TempData = tempDataMock.Object;

        var result = await controller.Activate("88");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal("88", redirect.RouteValues?["id"]);

        Assert.Equal(Projeto_SEGUES.Models.Enums.UserStatus.Active, pedro.Status);
        Assert.Null(pedro.LockoutEnd);
    }

    [Fact]
    public async Task ExportUsersPdf_GeneratesFileFromActualDatabaseData()
    {
        var (context, userManager, roleManager) = MockHelper.GetIdentitySetup();

        var adminServiceMock = new Mock<IAdminService>();
        var pdfServiceMock = new Mock<IPdfService>();

        var userList = new List<UserDto>
        {
            new UserDto { Id = "77", FullName = "Pedro PDF", Email = "pedro@pdf.pt" }
        };

        adminServiceMock.Setup(s => s.GetFilteredUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(userList);

        pdfServiceMock.Setup(s => s.GenerateAdminUsersListPdfAsync(userList, It.IsAny<string>()))
            .Returns(new byte[] { 0x10, 0x20, 0x30 });

        var controller = new AdminUserManagementController(
            userManager, 
            adminServiceMock.Object,
            Mock.Of<IUserService>(),
            Mock.Of<ILogger<AdminUserManagementController>>(),
            pdfServiceMock.Object);

        var result = await controller.ExportUsersPdf(new UserSearchViewModel());

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal(3, fileResult.FileContents.Length);
    }
}
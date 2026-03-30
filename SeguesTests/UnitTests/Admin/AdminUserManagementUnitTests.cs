using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.UnitTests.Admin;

public class AdminUserManagementUnitTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IAdminService> _adminServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<AdminUserManagementController>> _loggerMock;
    private readonly Mock<IPdfService> _pdfServiceMock;
    private readonly AdminUserManagementController _controller;

    public AdminUserManagementUnitTests()
    {
        var storeMock = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _adminServiceMock = new Mock<IAdminService>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<AdminUserManagementController>>();
        _pdfServiceMock = new Mock<IPdfService>();

        _controller = new AdminUserManagementController(
            _userManagerMock.Object,
            _adminServiceMock.Object,
            _userServiceMock.Object,
            _loggerMock.Object,
            _pdfServiceMock.Object);

        _controller.TempData = new Mock<ITempDataDictionary>().Object;
    }

    [Fact]
    public async Task Index_ReturnsView_WithPopulatedDropdowns()
    {
        _adminServiceMock.Setup(s => s.GetAllRolesForDropdownAsync()).ReturnsAsync(new List<SelectListItem>());
        _adminServiceMock.Setup(s => s.GetAllCategoriesForDropdownAsync()).ReturnsAsync(new List<SelectListItem>());
        _adminServiceMock.Setup(s => s.GetFilteredUsersAsync(null, null, null)).ReturnsAsync(new List<UserDto>());

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminUserManagementViewModel>(viewResult.Model);
        Assert.NotNull(model.Roles);
        Assert.NotNull(model.Categories);
        Assert.NotNull(model.SearchModel.Results);
    }

    [Fact]
    public async Task Edit_Get_UserNotFound_RedirectsToError()
    {
        _userServiceMock.Setup(s => s.GetUserForEditAsync("invalid-id")).ReturnsAsync((AppUser)null!);

        var result = await _controller.Edit("invalid-id");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Edit_Post_InvalidModelState_ReturnsViewWithDropdowns()
    {
        _controller.ModelState.AddModelError("FirstName", "Required");
        _adminServiceMock.Setup(s => s.GetAllRolesForDropdownAsync()).ReturnsAsync(new List<SelectListItem>());

        var model = new EditUserAdminViewModel
        {
            Id = "1",
            Role = "student",
            Category = "Student" ,
            FirstName = "Pedro",
            LastName = "T",
            Email = "p@t.pt",
            BirthDate = DateTime.Now,
            Gender = Gender.Male
        };

        var result = await _controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<EditUserAdminViewModel>(viewResult.Model);
        Assert.NotNull(returnedModel.RolesList);
    }

    [Fact]
    public async Task Deactivate_UserNotFound_RedirectsToIndexWithError()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("invalid")).ReturnsAsync((AppUser)null!);

        var result = await _controller.Deactivate("invalid");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ExportUsersPdf_ReturnsFileResult()
    {
        _adminServiceMock.Setup(s => s.GetFilteredUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UserDto>());

        _pdfServiceMock.Setup(s => s.GenerateAdminUsersListPdfAsync(It.IsAny<List<UserDto>>(), It.IsAny<string>()))
            .Returns(new byte[] { 1, 2, 3 });

        var searchModel = new UserSearchViewModel();

        var result = await _controller.ExportUsersPdf(searchModel);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Contains("Listagem_Utilizadores", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportStaffLogPdf_ReturnsFileResult()
    {
        _adminServiceMock.Setup(s => s.GetStaffLogFilteredAsync(It.IsAny<string>(), It.IsAny<UserAction?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<StaffLogDto>());

        _pdfServiceMock.Setup(s => s.GenerateAdminStaffLogPdfAsync(It.IsAny<List<StaffLogDto>>(), It.IsAny<string>()))
            .Returns(new byte[] { 1, 2, 3 });

        var model = new StaffLogSearchViewModel();

        var result = await _controller.ExportStaffLogPdf(model);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Contains("Historico_Funcionarios", fileResult.FileDownloadName);
    }
}
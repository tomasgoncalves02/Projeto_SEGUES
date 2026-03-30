using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Security.Claims;

namespace SeguesTests.RegressionTests.Admin;

public class AdminOrderManagementRegressionTests
{
    [Fact]
    public async Task UpdateSchedule_ServiceFailure_ShouldMaintainStateAndShowError()
    {
        var adminServiceMock = new Mock<IAdminService>();
        var controller = new AdminOrderManagementController(
            adminServiceMock.Object,
            Mock.Of<IReportService>(),
            Mock.Of<IPdfService>());

        var tempDataMock = new Mock<ITempDataDictionary>();
        controller.TempData = tempDataMock.Object;

        adminServiceMock.Setup(s => s.UpdateScheduleAsync(It.IsAny<BarCanteenConfigViewModel>()))
            .ReturnsAsync(ServiceResult.Fail("Horário inválido para o Pedro"));

        var result = await controller.UpdateOpenAndCloseTime(TimeSpan.FromHours(8), TimeSpan.FromHours(8));

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        tempDataMock.VerifySet(t => t[It.Is<string>(k => k.Contains("Swal"))] = It.IsAny<object>(), Times.Once);
    }

    [Fact]
    public async Task OrderHistory_FilterPersistence_ShouldReturnSpecificPedroResults()
    {
        var (context, _, _) = MockHelper.GetIdentitySetup();

        var category = new UserCategory { Name = "Estudante" };
        context.UserCategory.Add(category);

        var pedro = new AppUser
        {
            Id = "pedro-77",
            FirstName = "Pedro",
            LastName = "Regression",
            Email = "pedro.reg@test.com",
            UserName = "pedro.reg@test.com",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = category,
            Balance = 100,
            Status = Projeto_SEGUES.Models.Enums.UserStatus.Active
        };

        var otherUser = new AppUser
        {
            Id = "other-id",
            FirstName = "Outro",
            LastName = "User",
            Email = "outro@test.com",
            UserName = "outro@test.com",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Other,
            UserCategory = category,
            Balance = 0,
            Status = Projeto_SEGUES.Models.Enums.UserStatus.Active
        };

        context.Users.AddRange(pedro, otherUser);
        context.Order.Add(new Order { AppUser = pedro, OrderDate = DateTime.Now, Status = Projeto_SEGUES.Models.Enums.OrderStatus.Delivered, RedemptionCode = "PEDRO_VALID" });
        context.Order.Add(new Order { AppUser = otherUser, OrderDate = DateTime.Now, Status = Projeto_SEGUES.Models.Enums.OrderStatus.Delivered, RedemptionCode = "OTHER_VALID" });
        await context.SaveChangesAsync();

        var reportService = new ReportService(context);
        var controller = new AdminOrderManagementController(Mock.Of<IAdminService>(), reportService, Mock.Of<IPdfService>());

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "pedro-77")], "mock");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var searchModel = new ReportOrderSearchViewModel { SearchString = "PEDRO" };

        var result = await controller.GetFilteredOrders(searchModel);

        var partialResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<Order>>(partialResult.Model);
        Assert.Single(model);
        Assert.Equal("PEDRO_VALID", model[0].RedemptionCode);
    }

    [Fact]
    public async Task PdfExport_Consistency_ShouldAlwaysIncludeProductDetails()
    {
        var reportServiceMock = new Mock<IReportService>();
        var pdfServiceMock = new Mock<IPdfService>();
        var controller = new AdminOrderManagementController(Mock.Of<IAdminService>(), reportServiceMock.Object, pdfServiceMock.Object);

        var searchModel = new ReportOrderSearchViewModel();

        await controller.ExportOrdersPdf(searchModel);

        reportServiceMock.Verify(s => s.GetAdminOrderHistoryAsync(It.IsAny<ReportOrderSearchViewModel>(), true), Times.Once);
    }
}
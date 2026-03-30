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
using Xunit;

namespace SeguesTests.IntegrationTests.Admin;

public class AdminOrderManagementIntegrationTests
{
    [Fact]
    public async Task DashboardEntry_StateVerification()
    {
        var (context, _, _) = MockHelper.GetIdentitySetup();

        var category = new UserCategory { Name = "Externo" };
        context.UserCategory.Add(category);

        var pedro = new AppUser
        {
            Id = "pedro-id",
            FirstName = "Pedro",
            LastName = "Admin",
            Email = "pedro@segues.pt",
            UserName = "pedro@segues.pt",
            BirthDate = new DateTime(1995, 5, 5),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = category,
            Balance = 0,
            Status = Projeto_SEGUES.Models.Enums.UserStatus.Active
        };

        var order = new Order
        {
            AppUser = pedro,
            OrderDate = DateTime.Now,
            Status = Projeto_SEGUES.Models.Enums.OrderStatus.Delivered,
            TotalValue = 15.50m,
            RedemptionCode = "PEDRO123"
        };

        context.Users.Add(pedro);
        context.Order.Add(order);
        await context.SaveChangesAsync();

        var reportService = new ReportService(context);
        var adminServiceMock = new Mock<IAdminService>();

        adminServiceMock.Setup(s => s.GetScheduleAsync())
            .ReturnsAsync(new BarCanteenConfigViewModel { BarOpeningTimeString = "08:00", BarClosingTimeString = "20:00" });

        var controller = new AdminOrderManagementController(
            adminServiceMock.Object,
            reportService,
            Mock.Of<IPdfService>());

        controller.TempData = new Mock<ITempDataDictionary>().Object;

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminOrderManagementViewModel>(viewResult.Model);
        Assert.Contains(model.SearchModel.Results, o => o.RedemptionCode == "PEDRO123");
    }

    [Fact]
    public async Task OperationalHoursAdjustment_PersistenceCheck()
    {
        var (context, _, _) = MockHelper.GetIdentitySetup();
        var adminServiceMock = new Mock<IAdminService>();

        var open = new TimeSpan(9, 0, 0);
        var close = new TimeSpan(21, 0, 0);

        adminServiceMock.Setup(s => s.UpdateScheduleAsync(It.IsAny<BarCanteenConfigViewModel>()))
            .ReturnsAsync(ServiceResult.Ok("Horário do Pedro atualizado"));

        var controller = new AdminOrderManagementController(
            adminServiceMock.Object,
            Mock.Of<IReportService>(),
            Mock.Of<IPdfService>());

        controller.TempData = new Mock<ITempDataDictionary>().Object;

        var result = await controller.UpdateOpenAndCloseTime(open, close);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        adminServiceMock.Verify(s => s.UpdateScheduleAsync(It.Is<BarCanteenConfigViewModel>(v => v.BarOpeningTime == open)), Times.Once);
    }

    [Fact]
    public async Task WeekendServiceStatus_ChangeValidation()
    {
        var (context, _, _) = MockHelper.GetIdentitySetup();
        var adminServiceMock = new Mock<IAdminService>();

        adminServiceMock.Setup(s => s.UpdateSpecificDayStatusAsync("Saturday", true))
            .ReturnsAsync(ServiceResult.Ok("Sábado ativado"));

        var controller = new AdminOrderManagementController(
            adminServiceMock.Object,
            Mock.Of<IReportService>(),
            Mock.Of<IPdfService>());

        var tempDataMock = new Mock<ITempDataDictionary>();
        controller.TempData = tempDataMock.Object;

        var result = await controller.UpdateWeekendStatus("Saturday", true);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        tempDataMock.VerifySet(t => t[It.Is<string>(k => k.Contains("Swal"))] = It.IsAny<object>(), Times.Once);
    }
}
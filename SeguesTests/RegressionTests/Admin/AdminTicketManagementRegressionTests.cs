using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Services;

namespace SeguesTests.RegressionTests.Admin;

public class AdminTicketManagementRegressionTests
{
    [Fact]
    public async Task UpdateSchedule_ServiceError_ShouldMaintainStateAndShowSwal()
    {
        var adminServiceMock = new Mock<IAdminService>();
        var controller = new AdminTicketManagementController(
            adminServiceMock.Object,
            Mock.Of<ITicketService>(),
            Mock.Of<IPdfService>());

        var tempDataMock = new Mock<ITempDataDictionary>();
        controller.TempData = tempDataMock.Object;

        adminServiceMock.Setup(s => s.UpdateScheduleAsync(It.IsAny<BarCanteenConfigViewModel>()))
            .ReturnsAsync(ServiceResult.Fail("Erro de horário do Pedro"));

        var result = await controller.UpdateSchedule("Almoço", TimeSpan.FromHours(14), TimeSpan.FromHours(12));

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        tempDataMock.VerifySet(t => t[It.Is<string>(k => k.Contains("Swal"))] = It.IsAny<object>(), Times.Once);
    }

    [Fact]
    public async Task UpdateValidity_NegativeValue_ShouldBeBlockedByService()
    {
        var adminServiceMock = new Mock<IAdminService>();
        var controller = new AdminTicketManagementController(
            adminServiceMock.Object,
            Mock.Of<ITicketService>(),
            Mock.Of<IPdfService>());

        var tempDataMock = new Mock<ITempDataDictionary>();
        controller.TempData = tempDataMock.Object;

        adminServiceMock.Setup(s => s.UpdateTicketValidityDaysAsync(-1))
            .ReturnsAsync(ServiceResult.Fail("Dias inválidos"));

        var result = await controller.UpdateValidity(-1);

        Assert.IsType<RedirectToActionResult>(result);
        tempDataMock.VerifySet(t => t[It.Is<string>(k => k.Contains("Swal"))] = It.IsAny<object>(), Times.Once);
    }

    [Fact]
    public async Task AuditTable_FilterIntegrity_ShouldPassCorrectModelToService()
    {
        var ticketServiceMock = new Mock<ITicketService>();
        var controller = new AdminTicketManagementController(
            Mock.Of<IAdminService>(),
            ticketServiceMock.Object,
            Mock.Of<IPdfService>());

        var searchModel = new ReportTicketSearchViewModel { SearchString = "PEDRO_REGRESSAO" };

        await controller.GetUpdatedAuditTable(searchModel);

        ticketServiceMock.Verify(s => s.GetTicketHistoryAsync(
            null,
            It.Is<ReportTicketSearchViewModel>(m => m.SearchString == "PEDRO_REGRESSAO")),
            Times.Once);
    }
}
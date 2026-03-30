using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using Xunit;

namespace SeguesTests.IntegrationTests.Admin;

public class AdminTicketManagementIntegrationTests
{
    [Fact]
    public async Task UpdateSchedule_Almoco_PersistsCorrectTimes()
    {
        var (context, _, _) = MockHelper.GetIdentitySetup();
        var adminServiceMock = new Mock<IAdminService>();

        var open = new TimeSpan(12, 30, 0);
        var close = new TimeSpan(14, 30, 0);

        adminServiceMock.Setup(s => s.UpdateScheduleAsync(It.IsAny<BarCanteenConfigViewModel>()))
            .ReturnsAsync(ServiceResult.Ok("Horário de Almoço do Pedro atualizado"));

        var controller = new AdminTicketManagementController(
            adminServiceMock.Object,
            Mock.Of<ITicketService>(),
            Mock.Of<IPdfService>());

        controller.TempData = new Mock<ITempDataDictionary>().Object;

        var result = await controller.UpdateSchedule("Almoço", open, close);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        adminServiceMock.Verify(s => s.UpdateScheduleAsync(It.Is<BarCanteenConfigViewModel>(v =>
            v.CanteenLunchOpeningTime == open && v.CanteenLunchClosingTime == close)), Times.Once);
    }

    [Fact]
    public async Task GetUpdatedAuditTable_ReturnsFilteredResultsFromDb()
    {
        var (context, _, _) = MockHelper.GetIdentitySetup();

        var user = new AppUser
        {
            Id = "p1",
            FirstName = "Pedro",
            LastName = "T",
            Email = "p@t.pt",
            UserName = "p@t.pt",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = new UserCategory { Name = "Estudante" }
        };

        var ticket = new Ticket
        {
            ValidationCode = "PEDRO_123",
            TicketPurchase = new TicketPurchase { AppUser = user },
            Owner = user,
            ExpirationDate = DateTime.Now.AddDays(10)
        };

        context.Users.Add(user);
        context.Ticket.Add(ticket);
        await context.SaveChangesAsync();

        var ticketService = new TicketService(context, Mock.Of<ILogger<TicketService>>());
        var controller = new AdminTicketManagementController(Mock.Of<IAdminService>(), ticketService, Mock.Of<IPdfService>());

        var searchModel = new ReportTicketSearchViewModel { SearchString = "PEDRO" };

        var result = await controller.GetUpdatedAuditTable(searchModel);

        var partial = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Ticket>>(partial.Model);
        Assert.Contains(model, t => t.ValidationCode == "PEDRO_123");
    }

    [Fact]
    public async Task ExportTicketsPdf_ReturnsFileWithContent()
    {
        var (context, _, _) = MockHelper.GetIdentitySetup();
        var ticketService = new TicketService(context, Mock.Of<ILogger<TicketService>>());
        var pdfServiceMock = new Mock<IPdfService>();

        pdfServiceMock.Setup(s => s.GenerateAdminTicketHistoryPdfAsync(It.IsAny<List<Ticket>>(), It.IsAny<string>()))
            .Returns(new byte[] { 0x20, 0x21, 0x22 });

        var controller = new AdminTicketManagementController(Mock.Of<IAdminService>(), ticketService, pdfServiceMock.Object);
        var searchModel = new ReportTicketSearchViewModel();

        var result = await controller.ExportTicketsPdf(searchModel);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal(3, fileResult.FileContents.Length);
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Ticket.ViewModels;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers; 
using System.Security.Claims;
using Projeto_SEGUES.Areas.Ticket.Controllers;

namespace SeguesTests.UnitTests.Tickets;

public class TicketControllerUnitTests
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly TicketController _controller;
    private readonly List<AppUser> _users;

    public TicketControllerUnitTests()
    {
        var pedro = MockHelper.CreateValidAppUser(); // pedro-77 is the default user id in this helper
        _users = [pedro];

        _mockUserManager = MockHelper.MockUserManager(_users);
        _mockTicketService = new Mock<ITicketService>();
        var mockAdminService = new Mock<IAdminService>();
        
        _controller = new TicketController(
            _mockUserManager.Object,
            _mockTicketService.Object,
            mockAdminService.Object);
        
        MockHelper.SetupControllerContext(_controller, pedro.UserName!, pedro.Id);
    }
    
    [Fact]
    public async Task Index_UserNotFound_ReturnsChallenge()
    {
        _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser) null!);

        var result = await _controller.Index();

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task ActiveTickets_ReturnsView_WithList()
    {
        var pedro = _users[0];

        var mockTickets = new List<Ticket>
        {
            new()
            {
                Owner = pedro,
                TicketPurchase = new TicketPurchase
                {
                    TransactionDate = DateTime.Now,
                    AppUser = pedro,
                    Quantity = 1,
                    Value = 2.50m
                }
            }
        };

        _mockTicketService.Setup(s => s.GetActiveTicketsAsync(pedro.Id))
            .ReturnsAsync(mockTickets);

        var result = await _controller.ActiveTickets();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
    }

    [Fact]
    public async Task GetUpdatedTickets_UserLogged_ReturnsPartialView()
    {
        _mockTicketService.Setup(s => s.GetUserTicketsAsync("pedro-77"))
            .ReturnsAsync([]);

        var result = await _controller.GetUpdatedTickets();

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_TicketTablePartial", partial.ViewName);
    }

    [Fact]
    public async Task TransferTickets_ServiceReturnsFailure_ReturnsViewWithError()
    {
        var model = new TransferTicketViewModel
        {
            RecipientEmail = "outro@test.com",
            SelectedTickets = ["T1"]
        };

        _mockTicketService.Setup(s => s.TransferTicketsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(ServiceResult.Fail("Saldo Insuficiente, Pedro!"));

        var result = await _controller.TransferTickets(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(_controller.TransferTicket), viewResult.ViewName);
        Assert.Contains("Saldo Insuficiente", _controller.TempData["SwalData"]?.ToString());
    }

    [Fact]
    public async Task TransferTickets_Success_RedirectsWithSuccessMessage()
    {
        var model = new TransferTicketViewModel
        {
            RecipientEmail = "amigo@test.com",
            SelectedTickets = ["T1"]
        };

        _mockTicketService.Setup(s => s.TransferTicketsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(ServiceResult.Ok("Transferencia concluida"));

        var result = await _controller.TransferTickets(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(_controller.TransferTicket), redirect.ActionName);

        var swalData = _controller.TempData["SwalData"]?.ToString() ?? "";
        Assert.Contains("success", swalData);
        Assert.Contains("conclui", swalData.ToLower());
    }

    [Fact]
    public async Task CheckTransferEligibility_RecipientNotFound_ReturnsJsonFailure()
    {
        _mockTicketService.Setup(s => s.CheckTransferEligibilityAsync(It.IsAny<string>(), "fantasma@test.com"))
            .ReturnsAsync(ServiceResult<string>.Fail("Utilizador não encontrado"));

        var result = await _controller.CheckTransferEligibility("fantasma@test.com");

        var jsonResult = Assert.IsType<JsonResult>(result);

        var success = jsonResult.Value?.GetType().GetProperty("success")?.GetValue(jsonResult.Value, null);
        Assert.Equal(false, success);
    }
}
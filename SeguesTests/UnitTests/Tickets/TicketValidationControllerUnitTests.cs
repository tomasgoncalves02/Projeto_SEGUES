using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Ticket.ViewModels;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Projeto_SEGUES.Areas.Ticket.Controllers;
using SeguesTests.Helpers;

namespace SeguesTests.UnitTests.Tickets;

public class TicketValidationControllerUnitTests
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly TicketValidationController _controller;

    public TicketValidationControllerUnitTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _mockUserManager = MockHelper.MockUserManager(new List<AppUser>());
        
        _controller = new TicketValidationController(_mockUserManager.Object, _mockTicketService.Object);
        
        MockHelper.SetupControllerContext(_controller, "pedro.staff", "pedro-staff-77");
    }

    private static AppUser CreatePedroStaff() => new()
    {
        Id = "pedro-staff-77",
        FirstName = "Pedro",
        LastName = "Staff",
        BirthDate = DateTime.Today.AddYears(-25),
        Gender = Gender.Male,
        UserCategory = new UserCategory { Name = "Employee"},
        Email = "pedro.staff@segues.pt",
        UserName = "pedro.staff"
    };

    [Fact]
    public async Task Index_Get_ReturnsViewWithModel()
    {
        _mockTicketService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
            .ReturnsAsync([]);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ValidateTicketViewModel>(viewResult.Model);
        Assert.NotNull(model.RecentTickets);
    }

    [Fact]
    public async Task Index_Post_InvalidModel_DoesNotValidate()
    {
        var model = new ValidateTicketViewModel { Code = "" };
        _controller.ModelState.AddModelError("Code", "Required");

        _mockTicketService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
            .ReturnsAsync([]);

        var result = await _controller.Index(model);

        Assert.IsType<ViewResult>(result);
        _mockTicketService.Verify(s => s.ValidateTicketAsync(It.IsAny<string>(), It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task Index_Post_ValidTicket_SetsSuccessAndClearsForm()
    {
        var pedro = CreatePedroStaff();
        var model = new ValidateTicketViewModel { Code = "TICKET123" };

        _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(pedro);
        _mockTicketService.Setup(s => s.ValidateTicketAsync("TICKET123", pedro))
            .ReturnsAsync(ServiceResult.Ok("Validado com sucesso"));

        _mockTicketService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
            .ReturnsAsync([]);

        var result = await _controller.Index(model);

        Assert.IsType<ViewResult>(result);
        Assert.Contains("success", _controller.TempData["SwalData"]?.ToString()?.ToLower());
        Assert.Empty(model.Code);
        Assert.True(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Index_Post_ExpiredTicket_SetsErrorMessage()
    {
        var pedro = CreatePedroStaff();
        var model = new ValidateTicketViewModel { Code = "EXPIRADO" };

        _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(pedro);
        _mockTicketService.Setup(s => s.ValidateTicketAsync("EXPIRADO", pedro))
            .ReturnsAsync(ServiceResult.Fail("Ticket expirado"));

        await _controller.Index(model);

        Assert.Contains("error", _controller.TempData["SwalData"]?.ToString()?.ToLower());
    }

    [Fact]
    public async Task Index_Post_UserSessionExpired_ReturnsChallenge()
    {
        _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser) null!);

        var result = await _controller.Index(new ValidateTicketViewModel { Code = "ANY" });

        Assert.IsType<ChallengeResult>(result);
    }
}
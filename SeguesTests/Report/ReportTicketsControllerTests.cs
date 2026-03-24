using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Report;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace SeguesTests.Report;

public class ReportTicketsControllerTests
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly ReportTicketsController _controller;
    private readonly Mock<ILogger<ReportTicketsController>> _mockLogger;

        
    public ReportTicketsControllerTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        var store = new Mock<IUserStore<AppUser>>();
        _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        _mockLogger = new Mock<ILogger<ReportTicketsController>>();

        _controller = new ReportTicketsController(_mockTicketService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }
/*
    private AppUser CreateValidUser(string id) => new AppUser
    {
        Id = id,
        FirstName = "Pedro",
        LastName = "Report",
        Email = "pedro@test.com",
        BirthDate = DateTime.Now.AddYears(-20),
        Gender = Gender.Male,
        UserCategory = new UserCategory { Name = "Estudante" }
    };

    // Verifies that the index view loads correctly and populates ViewData with current filter values
    [Fact]
    public async Task Index_AuthenticatedUser_ReturnsViewWithFilters()
    {
        var user = CreateValidUser("u-123");
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);
        _mockTicketService.Setup(s => s.QueryHistoryAsync(user.Id, "search", TicketState.Available, "buy", null))
            .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

        var result = await _controller.Index("search", TicketState.Available, "buy", null);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("search", _controller.ViewData["CurrentSearch"]);
        Assert.Equal(TicketState.Available, _controller.ViewData["CurrentState"]);
        Assert.Equal(user.Id, _controller.ViewBag.CurrentUserId);
    }

    // Returns a ChallengeResult if the user ID cannot be retrieved during history access
    [Fact]
    public async Task Index_UserNotFound_ReturnsChallenge()
    {
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string)null!);

        var result = await _controller.Index(null, null, null, null);

        Assert.IsType<ChallengeResult>(result);
    }

    // Confirms that the filtered history returns a partial view for dynamic UI updates via HTMX
    [Fact]
    public async Task GetFilteredHistory_ValidEnum_ReturnsPartialView()
    {
        var userId = "u-123";
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockTicketService.Setup(s => s.QueryHistoryAsync(userId, It.IsAny<string>(), TicketState.Used, It.IsAny<string>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

        // Testing enum parsing from string "Used"
        var result = await _controller.GetFilteredHistory("Used", null, "all", "");

        var partialResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_TicketHistoryRows", partialResult.ViewName);
    }

    // Ensures the system handles invalid enum strings gracefully by defaulting the state filter to null
    [Fact]
    public async Task GetFilteredHistory_InvalidEnum_CallsServiceWithNullState()
    {
        var userId = "u-123";
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

        await _controller.GetFilteredHistory("InvalidStateName", null, "all", "");

        _mockTicketService.Verify(s => s.QueryHistoryAsync(userId, "", null, "all", null), Times.Once);
    }


    // Ensures that the selected date filter is correctly persisted in ViewData to maintain UI state
    [Fact]
    public async Task Index_VerifiesDateFilterPersistence()
    {
        var userId = "u-123";
        var testDate = new DateTime(2026, 05, 20);
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockTicketService.Setup(s => s.QueryHistoryAsync(userId, null, null, null, testDate))
            .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

        var result = await _controller.Index(null, null, null, testDate);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(testDate, _controller.ViewData["CurrentDate"]);
    }


    // Verifies that all filter parameters (search, state, flow, and date) are accurately mapped and passed to the ticket service
    [Fact]
    public async Task GetFilteredHistory_VerifiesExactMappingToService()
    {
        var userId = "u-123";
        var search = "Pedro-Search";
        var flow = "Received";
        var date = DateTime.Today;

        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockTicketService.Setup(s => s.QueryHistoryAsync(userId, search, TicketState.Available, flow, date))
            .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

        await _controller.GetFilteredHistory("Available", date, flow, search);

        _mockTicketService.Verify(s => s.QueryHistoryAsync(userId, search, TicketState.Available, flow, date), Times.Once);
    }*/
}
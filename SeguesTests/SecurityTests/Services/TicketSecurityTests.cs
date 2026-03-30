using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.SecurityTests.Services;

public class TicketSecurityTests
{
    private static AppDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AppUser CreatePedro(string id, string catName) => new()
    {
        Id = id,
        FirstName = "Pedro",
        LastName = "Security",
        Email = $"{id}@test.pt",
        UserName = $"{id}@test.pt",
        Balance = 100m,
        BirthDate = DateTime.Now.AddYears(-20),
        Gender = Gender.Male,
        UserCategory = new UserCategory { Name = catName }
    };

    [Fact]
    public async Task BuyTicketsAsync_InsufficientBalance_ReturnsFailure()
    {
        var context = GetContext();
        var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

        var cat = new UserCategory { Id = 1, Name = "Student" };
        var user = CreatePedro("u1", "Student");
        user.UserCategory = cat;
        user.Balance = 1m; 

        context.Users.Add(user);
        context.TicketPrice.Add(new TicketPrice { Price = 5m, UserCategory = cat, InitialDatePrice = DateTime.Now.AddDays(-1) });
        await context.SaveChangesAsync();

        var result = await service.BuyTicketsAsync("u1", 1);

        Assert.False(result.Success);
        Assert.Equal("Saldo insuficiente para a operação.", result.Message);
    }

    [Fact]
    public async Task CheckTransferEligibilityAsync_DifferentCategories_ReturnsFailure()
    {
        var context = GetContext();
        var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

        var sender = CreatePedro("s1", "Student");
        var receiver = CreatePedro("r1", "Employee"); 

        context.Users.AddRange(sender, receiver);
        await context.SaveChangesAsync();

        var result = await service.CheckTransferEligibilityAsync("s1", "r1@test.pt");

        Assert.False(result.Success);
        Assert.Contains("Transferência recusada", result.Message);
    }

    [Fact]
    public async Task TransferTicketsAsync_TicketNotOwnedBySender_ReturnsFailure()
    {
        var context = GetContext();
        var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

        var cat = new UserCategory { Id = 1, Name = "Student" };

        var sender = CreatePedro("s1", "Student");
        sender.UserCategory = cat;

        var receiver = CreatePedro("r1", "Student");
        receiver.UserCategory = cat;

        var hacker = CreatePedro("h1", "Student");
        hacker.UserCategory = cat;

        context.Users.AddRange(sender, receiver, hacker);

        var ticket = new Ticket
        {
            ValidationCode = "STOLEN",
            Owner = hacker, 
            State = TicketState.Available,
            ExpirationDate = DateTime.Now.AddDays(1),
            TicketPurchase = new TicketPurchase { AppUser = hacker, Quantity = 1, Value = 2m, TransactionDate = DateTime.Now }
        };

        context.Ticket.Add(ticket);
        await context.SaveChangesAsync();

        var result = await service.TransferTicketsAsync("s1", "r1@test.pt", ["STOLEN"]);

        Assert.False(result.Success);
        Assert.Contains("já não lhe pertencem", result.Message);
    }

    [Fact]
    public async Task ValidateTicketAsync_AlreadyUsed_ReturnsFailure()
    {
        var context = GetContext();
        var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());
        var pedro = CreatePedro("p1", "S");

        var staff = CreatePedro("staff1", "Employee");

        var ticket = new Ticket
        {
            ValidationCode = "USED-123",
            State = TicketState.Used,
            IsUsed = true,
            UsedDate = DateTime.Now.AddHours(-1),
            Owner = pedro,
            ExpirationDate = DateTime.Now.AddDays(1),
            TicketPurchase = new TicketPurchase { AppUser = pedro, Quantity = 1, Value = 2, TransactionDate = DateTime.Now }
        };

        context.Ticket.Add(ticket);
        await context.SaveChangesAsync();

        var result = await service.ValidateTicketAsync("USED-123", staff);

        Assert.False(result.Success);
        Assert.Contains("já utilizado", result.Message);
    }

    [Fact]
    public async Task ValidateTicketAsync_Expired_ReturnsFailure()
    {
        var context = GetContext();
        var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());
        var pedro = CreatePedro("p1", "S");
        var staff = CreatePedro("staff1", "Employee");

        var ticket = new Ticket
        {
            ValidationCode = "EXPIRED-99",
            State = TicketState.Available,
            ExpirationDate = DateTime.Now.AddDays(-1),
            Owner = pedro,
            TicketPurchase = new TicketPurchase { AppUser = pedro, Quantity = 1, Value = 2, TransactionDate = DateTime.Now }
        };

        context.Ticket.Add(ticket);
        await context.SaveChangesAsync();

        var result = await service.ValidateTicketAsync("EXPIRED-99", staff);

        Assert.False(result.Success);
        Assert.Equal("Bilhete expirado.", result.Message);
        Assert.Equal(TicketState.Expired, ticket.State);
    }
}
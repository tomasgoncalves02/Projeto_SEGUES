using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;

namespace SeguesTests.UnitTests.Services;

public class ReportUnitTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ReportService _service;
    
    public ReportUnitTests()
    {
        // Set up a fresh In-Memory database for every test
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new AppDbContext(options);
        _service = new ReportService(_context);
    }

    [Fact]
    public async Task GetOrdersStats_CalculatesAveragesCorrectly()
    {
        var pedro = MockHelper.CreateValidAppUser(); // pedro-77 is default
        _context.Users.Add(pedro);
        
        var now = DateTime.Now;
        _context.Order.AddRange(
            new Order { AppUser = pedro, TotalValue = 10m, OrderDate = now, Status = OrderStatus.Delivered, RedemptionCode = "R1" },
            new Order { AppUser = pedro, TotalValue = 20m, OrderDate = now, Status = OrderStatus.Delivered, RedemptionCode = "R2" }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetOrdersStats(); // 1 is default

        Assert.Equal(30m, result.TotalRevenue);
        Assert.Equal(15m, result.AverageRevenue);
        Assert.Equal(2, result.TotalOrders);
    }

    [Fact]
    public async Task GetTicketsStats_CalculatesRevenuePerTicket()
    {
        var pedro = MockHelper.CreateValidAppUser();
        _context.Users.Add(pedro);
        
        var now = DateTime.Now;
        var purchase = new TicketPurchase
        {
            AppUser = pedro, Quantity = 2, Value = 5.0m, TransactionDate = now
        };

        _context.Ticket.Add(new Ticket
        {
            ValidationCode = "T1", IsUsed = true, UsedDate = now, Owner = pedro, TicketPurchase = purchase, State = TicketState.Used, ExpirationDate = now.AddDays(1)
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetTicketsStats();

        Assert.Equal(2.5m, result.TotalRevenue);
    }
    
    // xUnit automatically calls Dispose() after each test finishes
    public void Dispose()
    {
        _context.Database.EnsureDeleted(); // Wipes the in-memory database
        _context.Dispose();                // Frees up the DbContext
    }
}
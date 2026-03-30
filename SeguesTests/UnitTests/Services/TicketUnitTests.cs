using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;

namespace SeguesTests.UnitTests.Services;

public class TicketUnitTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TicketService _service;
    
    public TicketUnitTests()
    {
        // Set up a fresh In-Memory database for every test
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new AppDbContext(options);
        _service = new TicketService(_context, Mock.Of<ILogger<TicketService>>());
    }

    [Fact]
    public async Task GetCurrentPriceForUserAsync_ReturnsMostRecentActivePrice()
    {
        var pedro = MockHelper.CreateValidAppUser();
        pedro.UserCategory.Name = "Pedro-Student";
        
        _context.Users.Add(pedro);
        var now = DateTime.Now;

        _context.TicketPrice.AddRange(
            new TicketPrice { Price = 2.0m, UserCategory = pedro.UserCategory, InitialDatePrice = now.AddDays(-10), EndDatePrice = now.AddDays(-5) },
            new TicketPrice { Price = 2.5m, UserCategory = pedro.UserCategory, InitialDatePrice = now.AddDays(-1), EndDatePrice = now.AddDays(1) }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetCurrentPriceForUserAsync(pedro);

        Assert.Equal(2.5m, result);
    }
    
    // xUnit automatically calls Dispose() after the test finishes
    public void Dispose()
    {
        _context.Database.EnsureDeleted(); // Wipes the in-memory database
        _context.Dispose();                // Frees up the DbContext
    }
}
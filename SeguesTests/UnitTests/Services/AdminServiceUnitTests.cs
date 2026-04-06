using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Services;

namespace SeguesTests.UnitTests.Services;

public class AdminServiceUnitTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AdminService _service;
    
    public AdminServiceUnitTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new AdminService(_context, null!, null!, null!, null!, null!);
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task UpdateScheduleAsync_ClosingBeforeOpening_ReturnsFailure()
    {
        _context.AppConfig.Add(new AppConfig { Id = 1 });
        await _context.SaveChangesAsync();
        
        var model = new BarCanteenConfigViewModel
        {
            BarOpeningTime = new TimeSpan(18, 0, 0),
            BarClosingTime = new TimeSpan(08, 0, 0)
        };

        var result = await _service.UpdateScheduleAsync(model);

        Assert.False(result.Success);
        Assert.Equal("A hora de fecho não pode ser anterior à hora de abertura.", result.Message);
    }

    [Fact]
    public async Task IsBarOpenAsync_ConditionalValidation_BasedOnCurrentDay()
    {
        var config = new AppConfig
        {
            BarOpeningTime = new TimeSpan(8, 0, 0),
            BarClosingTime = new TimeSpan(20, 0, 0),
            IsOpenSaturday = false, 
            IsOpenSunday = false    
        };
        _context.AppConfig.Add(config);
        await _context.SaveChangesAsync();
        
        var horaTeste = new TimeSpan(10, 0, 0); 

        var result = await _service.IsBarOpenAsync(horaTeste);
        var hoje = DateTime.Now.DayOfWeek;
        
        if (hoje is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            Assert.False(result, $"Hoje é {hoje}, o bar deveria estar FECHADO.");
        }
        else
        {
            Assert.True(result, $"Hoje é {hoje}, o bar deveria estar ABERTO às {horaTeste}.");
        }
    }
}
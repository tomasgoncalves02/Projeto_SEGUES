using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Stripe;


namespace Projeto_SEGUES.Services;

public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _context;

    public StatisticsService(AppDbContext context)
    {
        _context = context;
    }


    private async Task<object> GetaMealsStatsAsync(DateTime start)
    {
        var count = await _context.Ticket
                .Where(t => t.IsUsed && t.UsedDate >= start)
                .CountAsync();

        return new object[] { count };
    }

    private async Task<object> GetaRevenueStatsAsync(DateTime start)
    {
        var revenue = await _context.TicketPurchase
            .Where(p => p.TransactionDate >= start)
            .SumAsync(p => p.Value);

        return new object[] { revenue };
    }

    private async Task<object> GetAverageRevenueStatsAsync(DateTime start)
    {
        var averageRevenue = await _context.TicketPurchase
            .Where(p => p.TransactionDate >= start)
            .AverageAsync(p => p.Value);

        return new object[] { averageRevenue };
    }

    private async Task<object> GetNewBuyersStatsAsync(DateTime start)
    {
        var newBuyers = await _context.TicketPurchase
            .Where(p => p.TransactionDate >= start)
            .Select(p => p.AppUser.Id)
            .Distinct()
            .CountAsync();

        return new object[] { newBuyers };
    }

    private async Task<object> GetInfoGraphStatsAsync(DateTime start, string period)
    {
        var tickets = await _context.Ticket
             .Where(t => t.IsUsed && t.UsedDate >= start)
             .ToListAsync();

        var chartData = tickets
            .GroupBy(t => period switch
            {
                "Dia" => t.UsedDate!.Value.ToString("HH:00"),
                "Semana" => t.UsedDate!.Value.ToString("dd/MM"),
                "Mês" => t.UsedDate!.Value.ToString("dd"),
                "Ano" => t.UsedDate!.Value.ToString("MMM"),
                _ => t.UsedDate!.Value.Year.ToString()
            })
            .OrderBy(g => g.Key)
            .Select(g => new {
                label = g.Key,
                count = g.Count() 
            })
            .ToList();

        return chartData;
    }

    private async Task<object> GetByCategoryAsync(DateTime start)
    {
        var tickets = await _context.Ticket
            .Include(t => t.Owner).ThenInclude(u => u.UserCategory)
            .Where(t => t.IsUsed && t.UsedDate >= start)
            .ToListAsync();

        return tickets
            .GroupBy(t => t.Owner.UserCategory.Name)
            .Select(g => new { category = g.Key, count = g.Count() })
            .OrderByDescending(g => g.count)
            .ToList();
    }

    
    public async Task<object> GetTicketsStats(string period = "Dia")
    {
        var now = DateTime.Now;

        DateTime start = period switch
        {
            "Semana" => now.Date.AddDays(-(int)now.DayOfWeek + 1),
            "Mês" => new DateTime(now.Year, now.Month, 1),
            "Ano" => new DateTime(now.Year, 1, 1),
            "Total" => DateTime.MinValue,
            _ => now.Date
        };


        return new
        {
            totalMeals = await GetaMealsStatsAsync(start),
            totalRevenue = await GetaRevenueStatsAsync(start),
            averageRevenue = await GetAverageRevenueStatsAsync(start),
            newBuyers = await GetNewBuyersStatsAsync(start),
            chart = await GetInfoGraphStatsAsync(start, period),
            byCategory = await GetByCategoryAsync(start)
        };
    }
}
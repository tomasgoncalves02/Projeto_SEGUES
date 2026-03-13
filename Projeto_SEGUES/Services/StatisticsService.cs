using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;


namespace Projeto_SEGUES.Services;

public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _context;

    public StatisticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetTicketsStatsAsync(string period = "Dia")
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

        var count = await _context.Ticket
            .Where(t => t.IsUsed && t.UsedDate >= start)
            .CountAsync();

        var revenue = await _context.TicketPurchase
            .Where(p => p.TransactionDate >= start)
            .SumAsync(p => p.Value);

        var averageRevenue = await _context.TicketPurchase
            .Where(p => p.TransactionDate >= start)
            .AverageAsync(p => p.Value);

        var newBuyers = await _context.TicketPurchase
            .Where(p => p.TransactionDate >= start)
            .Select(p => p.AppUser.Id)
            .Distinct()
            .CountAsync();


        var tickets = await _context.Ticket
            .Where(t => t.IsUsed && t.UsedDate >= start)
            .ToListAsync();

                var chart = tickets
                    .GroupBy(t => period switch
                    {
                        "Dia" => t.UsedDate!.Value.ToString("HH:mm"),
                        "Semana" => t.UsedDate!.Value.DayOfWeek switch
                        {
                            DayOfWeek.Monday => "Seg",
                            DayOfWeek.Tuesday => "Ter",
                            DayOfWeek.Wednesday => "Qua",
                            DayOfWeek.Thursday => "Qui",
                            DayOfWeek.Friday => "Sex",
                            DayOfWeek.Saturday => "Sáb",
                            _ => "Dom"
                        },
                        "Mês" => t.UsedDate!.Value.Day.ToString(),
                        "Ano" => new DateTime(2000, t.UsedDate!.Value.Month, 1).ToString("MMM"),
                        _ => t.UsedDate!.Value.Year.ToString()
                    })
                    .OrderBy(g => g.Key)
                    .Select(g => new { label = g.Key, count = g.Count() })
                    .ToList();

        var tickets2 = await _context.Ticket
            .Include(t => t.Owner).ThenInclude(u => u.UserCategory)
            .Where(t => t.IsUsed && t.UsedDate >= start)
            .ToListAsync();

        var byCategory = tickets2
            .GroupBy(t => t.Owner.UserCategory.Name)
            .Select(g => new { category = g.Key, count = g.Count() })
            .OrderByDescending(g => g.count)
            .ToList();


        return new { totalMeals = count, totalRevenue = revenue, averageRevenue = averageRevenue, newBuyers = newBuyers, chart = chart, byCategory = byCategory };
    }

    




}
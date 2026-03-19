using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;


namespace Projeto_SEGUES.Services;

public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _context;

    public StatisticsService(AppDbContext context)
    {
        _context = context;
    }

    //Refeitório
    private async Task<object> GetaMealsStatsAsync(DateTime start)
    {
        return await _context.Ticket
                .Where(t => t.IsUsed && t.UsedDate >= start)
                .CountAsync();
    }

    private async Task<decimal> GetaRevenueStatsAsync(DateTime start)
    {
        var usedTickets = await _context.Ticket
            .Include(t => t.TicketPurchase)
            .Where(t => t.IsUsed && t.UsedDate >= start)
            .ToListAsync();

        if (!usedTickets.Any()) return 0m;

        return usedTickets.Sum(t =>
            t.TicketPurchase != null
                ? t.TicketPurchase.Value / t.TicketPurchase.Quantity
                : 0m);
    }

    private async Task<decimal> GetAverageRevenueStatsAsync(DateTime start)
    {
        var usedTickets = await _context.Ticket
            .Include(t => t.TicketPurchase)
            .Where(t => t.IsUsed && t.UsedDate >= start)
            .ToListAsync();

        if (!usedTickets.Any()) return 0m;

        var totalRevenue = usedTickets.Sum(t =>
            t.TicketPurchase != null
                ? t.TicketPurchase.Value / t.TicketPurchase.Quantity
                : 0m);

        return totalRevenue / usedTickets.Count;
    }

    private async Task<int> GetNewBuyersStatsAsync(DateTime start)
    {

        return await _context.Ticket
            .Where(t => t.IsUsed && t.UsedDate >= start)
            .Select(t => t.Owner.Id)
            .Distinct()
            .CountAsync();
    }

    private async Task<object> GetInfoGraphStatsAsync(DateTime start, int period)
    {
        var tickets = await _context.Ticket
             .Where(t => t.IsUsed && t.UsedDate != null && t.UsedDate >= start)
             .ToListAsync();


        if (!tickets.Any())
            return new List<object>();

        var chartData = tickets
            .GroupBy(t => period switch
            {
                1 => new { Order = t.UsedDate!.Value.Hour, Label = t.UsedDate!.Value.ToString("HH:00") },
                2 => new { Order = (int)t.UsedDate!.Value.DayOfWeek!, Label = t.UsedDate!.Value.ToString("dd/MM") },
                3 => new { Order = t.UsedDate!.Value.Day, Label = t.UsedDate!.Value.ToString("dd/MM") },
                4 => new { Order = t.UsedDate!.Value.Month, Label = t.UsedDate!.Value.ToString("MMMM") },
                5 => new { Order = t.UsedDate!.Value.Month, Label = t.UsedDate!.Value.ToString("MM/yyyy") },
                _ => new { Order = t.UsedDate!.Value.Year, Label = t.UsedDate!.Value.Year.ToString() }
            })
            .OrderBy(g => g.Key.Order)
            .Select(g => new
            {
                label = g.Key.Label,
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


    public async Task<object> GetTicketsStats(int period = 1)
    {
        var now = DateTime.Now;

        DateTime start = period switch
        {
            1 => now.Date,
            2 => now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Sunday),
            3 => new DateTime(now.Year, now.Month, 1),
            4 => new DateTime(now.Year, 1, 1),
            5 => DateTime.MinValue,
            _ => now.Date
        };
        
        return new
        {
            totalMeals = await GetaMealsStatsAsync(start),
            totalRevenue = (await GetaRevenueStatsAsync(start)).ToString("C"),
            averageRevenue = (await GetAverageRevenueStatsAsync(start)).ToString("C"),
            newBuyers = await GetNewBuyersStatsAsync(start),
            chart = await GetInfoGraphStatsAsync(start, period),
            byCategory = await GetByCategoryAsync(start)
        };
    }



    //Bar
    private async Task<int> GetConsuptionStatsAsync(DateTime start)
    {
        return await _context.Order
            .Where(o => o.OrderDate >= start && o.Status != OrderStatus.Cart && o.Status != OrderStatus.Cancelled)
            .CountAsync();
    }

    private async Task<decimal> GetRevenueBarStatsAsync(DateTime start)
    {
        return await _context.Order
            .Where(o => o.OrderDate >= start && o.Status != OrderStatus.Cart && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalValue);
    }

    private async Task<decimal> GetAverageBuyStatsAsync(DateTime start)
    {
        var orders = await _context.Order
            .Where(o => o.OrderDate >= start && o.Status != OrderStatus.Cart && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        if (!orders.Any()) return 0m;

        return orders.Average(o => o.TotalValue);
    }

    private async Task<int> GetNewBarUsersStatsAsync(DateTime start)
    {
        return await _context.Order
            .Where(o => o.OrderDate >= start && o.Status != OrderStatus.Cart && o.Status != OrderStatus.Cancelled)
            .Select(o => o.AppUser.Id)
            .Distinct()
            .CountAsync();
    }

    private async Task<object> GetBarGraphStatsAsync(DateTime start, int period)
    {
        var orders = await _context.Order
             .Where(o => o.OrderDate >= start && o.Status != OrderStatus.Cart && o.Status != OrderStatus.Cancelled)
             .ToListAsync();

        if (!orders.Any())
            return new List<object>();

        var chartData = orders
            .GroupBy(o => period switch
            {
                1 => new { Order = o.OrderDate.Hour, Label = o.OrderDate.ToString("HH:00") },
                2 => new { Order = (int)o.OrderDate.DayOfWeek, Label = o.OrderDate.ToString("dd/MM") },
                3 => new { Order = o.OrderDate.Day, Label = o.OrderDate.ToString("dd/MM") },
                4 => new { Order = o.OrderDate.Month, Label = o.OrderDate.ToString("MMMM") },
                5 => new { Order = o.OrderDate.Month, Label = o.OrderDate.ToString("MM/yyyy") },
                _ => new { Order = o.OrderDate.Year, Label = o.OrderDate.Year.ToString() }
            })
            .OrderBy(g => g.Key.Order)
            .Select(g => new
            {
                label = g.Key.Label,
                count = g.Count()
            })
            .ToList();

        return chartData;
    }

    private async Task<object> GetProductCategoryStatsAsync(DateTime start)
    {

        var categoryData = await _context.OrderLine
            .Where(ol => ol.Order.OrderDate >= start && ol.Order.Status != OrderStatus.Cart && ol.Order.Status != OrderStatus.Cancelled)
            .GroupBy(ol => ol.Product.Category.Name)
            .Select(g => new
            {
                category = g.Key,
                count = g.Sum(ol => ol.Quantity)
            })
            .ToListAsync();

        return categoryData;
    }

    private async Task<object> GetTopProductsStatsAsync(DateTime start)
    {
        var topProducts = await _context.OrderLine
             .Where(ol => ol.Order.OrderDate >= start && ol.Order.Status != OrderStatus.Cart && ol.Order.Status != OrderStatus.Cancelled && ol.Product.IsActive)
             .GroupBy(ol => ol.Product.Name)
             .Select(g => new
             {
                 name = g.Key,
                 quantity = g.Sum(ol => ol.Quantity)
             })
             .OrderByDescending(p => p.quantity)
             .ToListAsync();

        return topProducts;
    }






    public async Task<object> GetOrdersStats(int period = 1)
    {
        var now = DateTime.Now;
        DateTime start = period switch
        {
            1 => now.Date,
            2 => now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Sunday),
            3 => new DateTime(now.Year, now.Month, 1),
            4 => new DateTime(now.Year, 1, 1),
            5 => DateTime.MinValue,
            _ => now.Date
        };

        return new
        {
            totalOrderBar = await GetConsuptionStatsAsync(start),
            totalIncomeBar = (await GetRevenueBarStatsAsync(start)).ToString("C"),
            averageIncomeBar = (await GetAverageBuyStatsAsync(start)).ToString("C"),
            totalBuyersBar = await GetNewBarUsersStatsAsync(start),
            orderChart = await GetBarGraphStatsAsync(start, period),
            productCategories = await GetProductCategoryStatsAsync(start),
            topProducts = await GetTopProductsStatsAsync(start)
        };
    }


}
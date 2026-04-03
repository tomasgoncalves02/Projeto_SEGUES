using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.Ticket;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Service implementation for data analysis, reporting, and history retrieval.
/// Aggregates complex data from orders, tickets, and transactions to provide 
/// statistical insights for administrative and user-facing dashboards.
/// </summary>
public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportService"/>.
    /// </summary>
    /// <param name="context">The primary database context.</param>
    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    #region General

    /// <summary>
    /// Calculates the starting date for a given statistical period index.
    /// </summary>
    /// <param name="period">Period index (1: Today, 2: Week, 3: Month, 4: Year, 5: All Time).</param>
    /// <returns>A DateTime representing the start of the interval.</returns>
    private static DateTime GetStartDateForPeriod(int period)
    {
        var now = DateTime.Now;
        return period switch
        {
            1 => now.Date,
            2 => now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Sunday),
            3 => new DateTime(now.Year, now.Month, 1),
            4 => new DateTime(now.Year, 1, 1),
            5 => DateTime.MinValue,
            _ => now.Date
        };
    }

    #endregion

    #region Orders

    /// <summary>
    /// Retrieves a list of orders (excluding carts and cancelled ones) starting from a specific date.
    /// </summary>
    private async Task<List<Order>> GetOrdersAsync(DateTime start)
    {
        return await _context.Order
            .Include(o => o.AppUser)
            .Include(o => o.ProductPurchases).ThenInclude(ol => ol.Product).ThenInclude(p => p.Category)
            .Where(o => o.OrderDate >= start && o.Status != OrderStatus.Cart && o.Status != OrderStatus.Cancelled)
            .ToListAsync();
    }

    /// <summary>
    /// Groups orders by time intervals (hours, days, months) based on the selected period.
    /// Prepares data for Chart.js bar graphs.
    /// </summary>
    private static List<ChartDataDto> GetBarGraphStatsAsync(List<Order> orders, int period)
    {
        if (orders.Count == 0) return [];

        return orders
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
            .Select(g => new ChartDataDto
            {
                Label = g.Key.Label,
                Count = g.Count()
            })
            .ToList();
    }

    /// <summary>
    /// Aggregates order volume by product category.
    /// </summary>
    private static List<CategoryDataDto> GetProductCategoryStatsAsync(List<Order> orders)
    {
        if (orders.Count == 0) return [];

        return orders
            .SelectMany(o => o.ProductPurchases)
            .GroupBy(ol => ol.Product.Category.Name)
            .Select(g => new CategoryDataDto
            {
                Category = g.Key,
                Count = g.Sum(ol => ol.Quantity)
            })
            .OrderByDescending(g => g.Count)
            .ToList();
    }

    /// <summary>
    /// Identifies the top-selling products by quantity across the provided order list.
    /// </summary>
    private static List<ProductDataDto> GetTopProductsStatsAsync(List<Order> orders)
    {
        if (orders.Count == 0) return [];
        return orders
            .SelectMany(o => o.ProductPurchases)          
            .GroupBy(ol => ol.Product.Name)
            .Select(g => new ProductDataDto
            {
                Name = g.Key,
                Quantity = g.Sum(ol => ol.Quantity)
            })
            .OrderByDescending(p => p.Quantity)
            .ToList();
    }

    /// <summary>
    /// Compiles a comprehensive statistics DTO for Bar Orders based on a time period.
    /// </summary>
    public async Task<ReportStatisticsOrderDto> GetOrdersStats(int period = 1)
    {
        var start = GetStartDateForPeriod(period);
        var orders = await GetOrdersAsync(start);
        int totalOrders = orders.Count;
        decimal totalRevenue = totalOrders == 0 ? 0m : orders.Sum(o => o.TotalValue);

        return new ReportStatisticsOrderDto
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            AverageRevenue = orders.Count == 0 ? 0m : totalRevenue / totalOrders,
            NumberOfBuyers = orders.Select(o => o.AppUser.Id).Distinct().Count(),
            OrderChart = GetBarGraphStatsAsync(orders, period),
            ProductCategories = GetProductCategoryStatsAsync(orders),
            TopProducts = GetTopProductsStatsAsync(orders)
        };
    }

    /// <summary>
    /// Constructs the base query for order history, allowing optional inclusion of product details.
    /// </summary>
    private IQueryable<Order> BuildOrderHistoryBaseQuery(string? userId = null, bool includeProducts = false)
    {
        var query = _context.Order
            .Include(o => o.AppUser)
            .Where(o => o.Status != OrderStatus.Cart);

        if (includeProducts)
            query = query.Include(o => o.ProductPurchases).ThenInclude(ol => ol.Product);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(o => o.AppUser.Id == userId);
        }

        return query.AsNoTracking().AsQueryable();
    }

    /// <summary>
    /// Applies search and status filters to an existing order query.
    /// </summary>
    private static IQueryable<Order> ApplyOrderHistorySearchFilters(IQueryable<Order> query, ReportOrderSearchViewModel model, bool filterOwner = false)
    {
        var searchString = model.SearchString?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(o =>
                o.RedemptionCode.ToLower().Contains(searchString) ||
                o.Id.ToString().Contains(searchString) ||
                (filterOwner && (o.AppUser.FirstName + " " + o.AppUser.LastName).ToLower().Contains(searchString))
            );
        }

        if (model.StatusFilter.HasValue)
        {
            query = query.Where(o => o.Status == model.StatusFilter.Value);
        }

        if (model.DateFilter.HasValue)
        {
            query = query.Where(o => o.OrderDate.Date == model.DateFilter.Value.Date);
        }

        return query;
    }

    /// <summary>
    /// Retrieves order history for a specific customer.
    /// </summary>
    public async Task<List<Order>> GetOrderHistoryAsync(string userId, ReportOrderSearchViewModel model)
    {
        var query = BuildOrderHistoryBaseQuery(userId);
        query = ApplyOrderHistorySearchFilters(query, model);
        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    /// <summary>
    /// Retrieves global order history for administrative review.
    /// </summary>
    public async Task<List<Order>> GetAdminOrderHistoryAsync(ReportOrderSearchViewModel model, bool includeProducts = false)
    {
        var query = BuildOrderHistoryBaseQuery(null, includeProducts);
        query = ApplyOrderHistorySearchFilters(query, model, true);
        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    #endregion

    #region Tickets

    /// <summary>
    /// Fetches used tickets with their associated purchase and owner category info.
    /// </summary>
    private async Task<List<Ticket>> GetUsedTicketsAsync(DateTime start)
    {
        return await _context.Ticket
            .Include(t => t.TicketPurchase)
            .Include(t => t.Owner).ThenInclude(u => u.UserCategory)
            .Where(t => t.IsUsed && t.UsedDate != null && t.UsedDate >= start)
            .ToListAsync();
    }

    /// <summary>
    /// Prepares data for canteen usage charts, grouping by hour, day, or month.
    /// </summary>
    private static List<ChartDataDto> GetInfoGraphStatsAsync(List<Ticket> tickets, int period)
    {
        if (tickets.Count == 0) return [];

        return tickets
            .GroupBy(t => period switch
            {
                1 => new { Order = t.UsedDate!.Value.Hour, Label = t.UsedDate!.Value.ToString("HH:00") },
                2 => new { Order = (int)t.UsedDate!.Value.DayOfWeek, Label = t.UsedDate!.Value.ToString("dd/MM") },
                3 => new { Order = t.UsedDate!.Value.Day, Label = t.UsedDate!.Value.ToString("dd/MM") },
                4 => new { Order = t.UsedDate!.Value.Month, Label = t.UsedDate!.Value.ToString("MMMM") },
                5 => new { Order = t.UsedDate!.Value.Month, Label = t.UsedDate!.Value.ToString("MM/yyyy") },
                _ => new { Order = t.UsedDate!.Value.Year, Label = t.UsedDate!.Value.Year.ToString() }
            })
            .OrderBy(g => g.Key.Order)
            .Select(g => new ChartDataDto
            {
                Label = g.Key.Label,
                Count = g.Count()
            })
            .ToList();
    }

    /// <summary>
    /// Breaks down meal consumption by user category (Student, Staff, etc.).
    /// </summary>
    private static List<CategoryDataDto> GetByCategoryAsync(List<Ticket> tickets)
    {
        if (tickets.Count == 0) return [];
        return tickets
            .GroupBy(t => t.Owner.UserCategory.Name)
            .Select(g => new CategoryDataDto
            {
                Category = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(g => g.Count)
            .ToList();
    }

    /// <summary>
    /// Compiles statistics for meal ticket usage within a given period.
    /// </summary>
    public async Task<ReportStatisticsTicketDto> GetTicketsStats(int period = 1)
    {
        var start = GetStartDateForPeriod(period);
        var tickets = await GetUsedTicketsAsync(start);
        int totalUsedTickets = tickets.Count;
        decimal totalRevenue = totalUsedTickets == 0 ? 0m : tickets.Sum(t => t.TicketPurchase.Value / t.TicketPurchase.Quantity);

        return new ReportStatisticsTicketDto
        {
            TotalUsedTickets = totalUsedTickets,
            TotalRevenue = totalRevenue,
            AverageRevenue = totalUsedTickets == 0 ? 0m : totalRevenue / totalUsedTickets,
            NumberOfBuyers = tickets.Select(t => t.Owner.Id).Distinct().Count(),
            Chart = GetInfoGraphStatsAsync(tickets, period),
            ByCategory = GetByCategoryAsync(tickets)
        };
    }

    #endregion

    #region Transactions

    /// <summary>
    /// Retrieves a detailed financial statement for a user, including top-ups and spending.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="model">Filters for reference/description, type (In/Out), and date.</param>
    public async Task<List<Transaction>> GetTransactionHistoryAsync(string userId, ReportTransactionSearchViewModel model)
    {
        var query = _context.Transaction
            .Include(t => t.User)
            .Where(t => t.User.Id == userId)
            .AsNoTracking()
            .AsQueryable();

        // Filter by Reference or Description
        var searchString = model.SearchString?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(t =>
                (t.Description != null && t.Description.ToLower().Contains(searchString))
                || t.Reference.ToLower().Contains(searchString)
            );
        }

        // Filter by Transaction Flow (Incoming vs Outgoing)
        var typeFilter = model.TypeFilter;
        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query = typeFilter switch
            {
                "Entrada" => query.Where(t => t.Amount > 0),
                "Saida" => query.Where(t => t.Amount < 0),
                _ => query
            };
        }

        // Filter by Date
        var dateFilter = model.DateFilter;
        if (dateFilter.HasValue)
        {
            query = query.Where(t => t.CreatedAt.Date >= dateFilter.Value.Date);
        }

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    #endregion
}
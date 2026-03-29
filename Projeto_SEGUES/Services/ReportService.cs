using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.Ticket;

namespace Projeto_SEGUES.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    #region General
    
    private DateTime GetStartDateForPeriod(int period)
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
    
    private async Task<List<Order>> GetOrdersAsync(DateTime start)
    {
        return await _context.Order
            .Include(o => o.AppUser)
            .Include(o => o.ProductPurchases).ThenInclude(ol => ol.Product).ThenInclude(p => p.Category)
            .Where(o => o.OrderDate >= start && o.Status != OrderStatus.Cart && o.Status != OrderStatus.Cancelled)
            .ToListAsync();
    }

    private List<ChartDataDto> GetBarGraphStatsAsync(List<Order> orders, int period)
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

    private List<CategoryDataDto> GetProductCategoryStatsAsync(List<Order> orders)
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

    private List<ProductDataDto> GetTopProductsStatsAsync(List<Order> orders)
    {
        if (orders.Count == 0) return [];
        return orders
            .SelectMany(o => o.ProductPurchases)
            .Where(ol => ol.Product.IsActive)
            .GroupBy(ol => ol.Product.Name)
            .Select(g => new ProductDataDto
            {
                 Name = g.Key,
                 Quantity = g.Sum(ol => ol.Quantity)
            })
            .OrderByDescending(p => p.Quantity)
            .ToList();
    }
    
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
    
    private IQueryable<Order> ApplyOrderHistorySearchFilters(IQueryable<Order> query, ReportOrderSearchViewModel model, bool filterOwner = false)
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
    
    public async Task<List<Order>> GetOrderHistoryAsync(string userId, ReportOrderSearchViewModel model)
    {
        var query = BuildOrderHistoryBaseQuery(userId);
        query = ApplyOrderHistorySearchFilters(query, model);
        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }
    
    public async Task<List<Order>> GetAdminOrderHistoryAsync(ReportOrderSearchViewModel model, bool includeProducts = false)
    {
        var query = BuildOrderHistoryBaseQuery(null, includeProducts);
        query = ApplyOrderHistorySearchFilters(query, model, true);
        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }
    
    #endregion

    #region Tickets

    private async Task<List<Ticket>> GetUsedTicketsAsync(DateTime start)
    {
        return await _context.Ticket
            .Include(t => t.TicketPurchase)
            .Include(t => t.Owner).ThenInclude(u => u.UserCategory)
            .Where(t => t.IsUsed && t.UsedDate != null && t.UsedDate >= start)
            .ToListAsync();
    }
    
    private List<ChartDataDto> GetInfoGraphStatsAsync(List<Ticket> tickets, int period)
    {
        if (tickets.Count == 0) return [];
        
        return tickets
            .GroupBy(t => period switch
            {
                1 => new { Order = t.UsedDate!.Value.Hour, Label = t.UsedDate!.Value.ToString("HH:00") },
                2 => new { Order = (int) t.UsedDate!.Value.DayOfWeek, Label = t.UsedDate!.Value.ToString("dd/MM") },
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

    private List<CategoryDataDto> GetByCategoryAsync(List<Ticket> tickets)
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

    public async Task<List<Transaction>> GetTransactionHistoryAsync(string userId, ReportTransactionSearchViewModel model)
    {
        var query = _context.Transaction
            .Include(t => t.User)
            .Where(t => t.User.Id == userId)
            .AsNoTracking()
            .AsQueryable();
        
        // Text Filter
        var searchString = model.SearchString?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(t =>
                (t.Description != null && t.Description.ToLower().Contains(searchString)) 
                || t.Reference.ToLower().Contains(searchString)
            );
        }
        
        // Flow filter (Entrada/Saída)
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
        
        // Date filter
        var dateFilter = model.DateFilter;
        if (dateFilter.HasValue)
        {
            query = query.Where(t => t.CreatedAt.Date >= dateFilter.Value.Date);
        }
        
        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }
    
    #endregion
}
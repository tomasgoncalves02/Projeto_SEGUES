using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.RegressionTests.Services;

public class ReportRegressionTests
{
    private static AppDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AppUser CreatePedro(string id) => new()
    {
        Id = id,
        FirstName = "Pedro",
        LastName = "Regression",
        Balance = 0m,
        BirthDate = DateTime.Now.AddYears(-20),
        Gender = Gender.Male,
        UserCategory = new UserCategory { Name = "Student" },
        Email = $"pedro.{id}@test.pt"
    };

    [Fact]
    public async Task GetOrdersStats_EmptyDatabase_ReturnsDefaultDtoWithoutErrors()
    {
        var context = GetContext();
        var service = new ReportService(context);

        var result = await service.GetOrdersStats();

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalOrders);
        Assert.Equal(0m, result.TotalRevenue);
        Assert.Equal(0m, result.AverageRevenue);
        Assert.Empty(result.TopProducts);
    }

    [Fact]
    public async Task GetTicketsStats_ZeroUsedTickets_AverageRevenueIsZero()
    {
        var context = GetContext();
        var service = new ReportService(context);

        var result = await service.GetTicketsStats();

        Assert.Equal(0, result.TotalUsedTickets);
        Assert.Equal(0m, result.AverageRevenue);
    }

    [Fact]
    public async Task GetOrderHistoryAsync_SearchByCode_IsCaseInsensitive()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("u1");
        context.Order.Add(new Order
        {
            AppUser = pedro,
            Status = OrderStatus.Delivered,
            OrderDate = DateTime.Now,
            RedemptionCode = "ABC-123",
            TotalValue = 10m
        });
        await context.SaveChangesAsync();

        var model = new ReportOrderSearchViewModel { SearchString = "abc-123" };
        var result = await service.GetOrderHistoryAsync("u1", model);

        Assert.Single(result);
        Assert.Equal("ABC-123", result[0].RedemptionCode);
    }

    [Fact]
    public async Task GetStartDateForPeriod_YearlyFilter_IncludesStartOfCurrentYear()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("u1");

        var earlyYearOrder = new Order
        {
            AppUser = pedro,
            Status = OrderStatus.Delivered,
            OrderDate = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 1),
            RedemptionCode = "NEW-YEAR",
            TotalValue = 50m
        };
        context.Order.Add(earlyYearOrder);
        await context.SaveChangesAsync();

        var result = await service.GetOrdersStats(4); 

        Assert.Equal(1, result.TotalOrders);
        Assert.Equal(50m, result.TotalRevenue);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_NoTransactionsFound_ReturnsEmptyListNotFiltered()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("u1");
        context.Users.Add(pedro);
        await context.SaveChangesAsync();

        var result = await service.GetTransactionHistoryAsync("u1", new ReportTransactionSearchViewModel());

        Assert.Empty(result);
    }
}
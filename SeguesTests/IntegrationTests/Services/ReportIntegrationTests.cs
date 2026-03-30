using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Services;

namespace SeguesTests.IntegrationTests.Services;

public class ReportIntegrationTests
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
        LastName = "Integration",
        Balance = 100m,
        BirthDate = DateTime.Now.AddYears(-20),
        Gender = Gender.Male,
        UserCategory = new UserCategory { Name = "Student" },
        Email = $"pedro.{id}@test.pt"
    };

    [Fact]
    public async Task GetOrdersStats_VerifiesComplexJoinsAndCalculations()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("u1");
        var cat = new ProductCategory { Name = "Bar-Pedro", Description = "D" };
        var prod = new Product { Name = "Cafe", Price = 1.5m, Category = cat, Stock = 10, MinimumStock = 1, Description = "D" };

        var order = new Order
        {
            AppUser = pedro,
            Status = OrderStatus.Delivered,
            OrderDate = DateTime.Now,
            TotalValue = 3.0m,
            RedemptionCode = "INT-1"
        };
        order.ProductPurchases.Add(new OrderLine
        {
            Product = prod,
            Quantity = 2,
            ProductValue = 1.5m,
            Order = order,
            ProductId = 0,
            OrderId = 0
        });

        context.Order.Add(order);
        await context.SaveChangesAsync();

        var result = await service.GetOrdersStats();

        Assert.Equal(3.0m, result.TotalRevenue);
        Assert.Single(result.ProductCategories);
        Assert.Equal("Bar-Pedro", result.ProductCategories[0].Category);
        Assert.Equal(2, result.ProductCategories[0].Count);
    }

    [Fact]
    public async Task GetTicketsStats_VerifiesUserCategoryRelation()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var cat = new UserCategory { Name = "Pedro-VIP" };
        var pedro = CreatePedro("u1");
        pedro.UserCategory = cat;

        var purchase = new TicketPurchase { AppUser = pedro, Quantity = 1, Value = 5m, TransactionDate = DateTime.Now };
        context.Ticket.Add(new Ticket
        {
            ValidationCode = "T-INT",
            IsUsed = true,
            UsedDate = DateTime.Now,
            Owner = pedro,
            TicketPurchase = purchase,
            State = TicketState.Used,
            ExpirationDate = DateTime.Now.AddDays(1)
        });
        await context.SaveChangesAsync();

        var result = await service.GetTicketsStats();

        Assert.Single(result.ByCategory);
        Assert.Equal("Pedro-VIP", result.ByCategory[0].Category);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_FiltersByFlowAndDatePersistently()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("u1");

        context.Transaction.AddRange(
            new Transaction { User = pedro, Amount = 50, Description = "Pedro-In", Reference = "REF1", CreatedAt = DateTime.Now, IsPaid = true },
            new Transaction { User = pedro, Amount = -10, Description = "Pedro-Out", Reference = "REF2", CreatedAt = DateTime.Now, IsPaid = true },
            new Transaction { User = pedro, Amount = 100, Description = "Old", Reference = "REF3", CreatedAt = DateTime.Now.AddMonths(-1), IsPaid = true }
        );
        await context.SaveChangesAsync();

        var model = new ReportTransactionSearchViewModel { TypeFilter = "Entrada", DateFilter = DateTime.Now.Date };
        var result = await service.GetTransactionHistoryAsync("u1", model);

        Assert.Single(result);
        Assert.Equal("Pedro-In", result[0].Description);
    }

    [Fact]
    public async Task GetOrderHistoryAsync_SearchByRedemptionCode_ReturnsCorrectRecord()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("u1");
        context.Order.Add(new Order { AppUser = pedro, Status = OrderStatus.Delivered, OrderDate = DateTime.Now, RedemptionCode = "FIND-ME", TotalValue = 10m });
        await context.SaveChangesAsync();

        var model = new ReportOrderSearchViewModel { SearchString = "find-me" };
        var result = await service.GetOrderHistoryAsync("u1", model);

        Assert.Single(result);
        Assert.Equal("FIND-ME", result[0].RedemptionCode);
    }
}
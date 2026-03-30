using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.SecurityTests.Services;

public class ReportSecurityTests
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
        LastName = "Security",
        Balance = 100m,
        BirthDate = DateTime.Now.AddYears(-20),
        Gender = Gender.Male,
        UserCategory = new UserCategory { Name = "Student" },
        Email = $"pedro.{id}@test.pt"
    };

    [Fact]
    public async Task GetOrderHistoryAsync_StrictUserIsolation_ReturnsOnlyOwnOrders()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("pedro-owner");
        var intruso = CreatePedro("intruso-id");

        context.Users.AddRange(pedro, intruso);
        context.Order.AddRange(
            new Order { AppUser = pedro, Status = OrderStatus.Delivered, OrderDate = DateTime.Now, RedemptionCode = "PEDRO-1", TotalValue = 10m },
            new Order { AppUser = intruso, Status = OrderStatus.Delivered, OrderDate = DateTime.Now, RedemptionCode = "OUTRO-1", TotalValue = 20m }
        );
        await context.SaveChangesAsync();

        var result = await service.GetOrderHistoryAsync("pedro-owner", new ReportOrderSearchViewModel());

        Assert.Single(result);
        Assert.Equal("PEDRO-1", result[0].RedemptionCode);
        Assert.All(result, o => Assert.Equal("pedro-owner", o.AppUser.Id));
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_PreventsCrossUserAccess()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("pedro-id");
        var outro = CreatePedro("outro-id");

        context.Transaction.AddRange(
            new Projeto_SEGUES.Models.Payment.Transaction { User = pedro, Amount = -10, Description = "Pedro-Pay", Reference = "REF1", CreatedAt = DateTime.Now, IsPaid = true },
            new Projeto_SEGUES.Models.Payment.Transaction { User = outro, Amount = -50, Description = "Outro-Pay", Reference = "REF2", CreatedAt = DateTime.Now, IsPaid = true }
        );
        await context.SaveChangesAsync();

        var result = await service.GetTransactionHistoryAsync("pedro-id", new ReportTransactionSearchViewModel());

        Assert.Single(result);
        Assert.Equal("Pedro-Pay", result[0].Description);
    }

    [Fact]
    public async Task GetOrdersStats_SecurityFilter_ExcludesNonFinalizedStates()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("u1");

        context.Order.AddRange(
            new Order { AppUser = pedro, Status = OrderStatus.Cart, TotalValue = 1000m, OrderDate = DateTime.Now, RedemptionCode = "CART" },
            new Order { AppUser = pedro, Status = OrderStatus.Cancelled, TotalValue = 500m, OrderDate = DateTime.Now, RedemptionCode = "VOID" },
            new Order { AppUser = pedro, Status = OrderStatus.Delivered, TotalValue = 10m, OrderDate = DateTime.Now, RedemptionCode = "REAL" }
        );
        await context.SaveChangesAsync();

        var stats = await service.GetOrdersStats();

        Assert.Equal(10m, stats.TotalRevenue);
        Assert.Equal(1, stats.TotalOrders);
    }

    [Fact]
    public async Task GetAdminOrderHistoryAsync_SearchFilter_SanitizesInput()
    {
        var context = GetContext();
        var service = new ReportService(context);
        var pedro = CreatePedro("u1");
        context.Order.Add(new Order { AppUser = pedro, Status = OrderStatus.Delivered, OrderDate = DateTime.Now, RedemptionCode = "PEDRO-CODE" });
        await context.SaveChangesAsync();

        var model = new ReportOrderSearchViewModel { SearchString = "   pedro-code   " };
        var result = await service.GetAdminOrderHistoryAsync(model);

        Assert.Single(result);
        Assert.Equal("PEDRO-CODE", result[0].RedemptionCode);
    }
}
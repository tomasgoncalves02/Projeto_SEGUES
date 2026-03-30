using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Identity.UI.Services;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.RegressionTests.Services;

public class OrderRegressionTests
{
    private static AppDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CancelOrderAsync_AlreadyCancelled_DoesNotRefundTwice()
    {
        var context = GetContext();
        var service = new OrderService(context, Mock.Of<IAdminService>(), Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

        var user = CreatePedro("u-1", balance: 50m);
        var order = new Order { Id = 1, AppUser = user, Status = OrderStatus.Cancelled, TotalValue = 10m };
        context.Users.Add(user);
        context.Order.Add(order);
        await context.SaveChangesAsync();

        var result = await service.CancelOrderAsync(1);

        Assert.False(result.Success);
        Assert.Equal(50m, user.Balance);
    }

    [Fact]
    public async Task RemoveFromCartAsync_TotalValueDoesNotDropBelowZero()
    {
        var context = GetContext();
        var service = new OrderService(context, Mock.Of<IAdminService>(), Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

        var user = CreatePedro("u-1");
        var cart = new Order { Id = 5, AppUser = user, Status = OrderStatus.Cart, TotalValue = 2m };
        var product = CreateProduct(1, "Pedro-Cafe", 5m);
        var line = new OrderLine
        {
            Order = cart,
            OrderId = 5,
            Product = product,
            ProductId = 1,
            Quantity = 1,
            ProductValue = 10m
        };

        context.Users.Add(user);
        context.Order.Add(cart);
        context.OrderLine.Add(line);
        await context.SaveChangesAsync();

        await service.RemoveFromCartAsync("u-1", 1);

        Assert.Equal(0, cart.TotalValue);
    }

    [Fact]
    public async Task SubmitOrderAsync_GeneratesNewCodeIfCollisionExists()
    {
        var context = GetContext();
        var adminMock = new Mock<IAdminService>();
        var service = new OrderService(context, adminMock.Object, Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

        var user = CreatePedro("u-1", balance: 100m);

        var existingOrder = new Order
        {
            Id = 10,
            AppUser = user,
            RedemptionCode = "PEDRO-12",
            TotalValue = 5m,
            Status = OrderStatus.Pending
        };

        var cart = new Order
        {
            Id = 11,
            AppUser = user,
            Status = OrderStatus.Cart,
            TotalValue = 10m,
            RedemptionCode = "PEDRO-12"
        };

        var product = CreateProduct(1, "Item", 10m);
        cart.ProductPurchases.Add(new OrderLine
        {
            Order = cart,
            OrderId = 11,
            Product = product,
            ProductId = 1,
            Quantity = 1,
            ProductValue = 10m
        });

        context.Users.Add(user);
        context.Order.AddRange(existingOrder, cart);
        await context.SaveChangesAsync();
        adminMock.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);

        await service.SubmitOrderAsync(user, true, null);

        Assert.NotEqual("PEDRO-12", cart.RedemptionCode);
        Assert.Equal(OrderStatus.Pending, cart.Status);
    }

    private static AppUser CreatePedro(string id, decimal balance = 0m) => new()
    {
        Id = id,
        FirstName = "Pedro",
        LastName = "Regression",
        Balance = balance,
        BirthDate = DateTime.Now.AddYears(-20),
        Gender = Gender.Male,
        UserCategory = new UserCategory { Name = "Student" }
    };

    private static Product CreateProduct(int id, string name, decimal price) => new()
    {
        Id = id,
        Name = name,
        Price = price,
        Stock = 100,
        MinimumStock = 5,
        Description = "D",
        Category = new ProductCategory { Name = "Bar", Description = "D" }
    };
}
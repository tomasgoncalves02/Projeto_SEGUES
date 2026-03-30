using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace SeguesTests.UnitTests.Services;

public class OrderUnitTests
{
    private readonly OrderService _service;
    
    public OrderUnitTests()
    {
        _service = new OrderService(null!, null!, null!, null!);
    }
    
    [Fact]
    public void ApplyDiscount_Percentage_CalculatesValueBasedOnCode()
    {
        var discount = new Discount
        {
            IsActive = true,
            DiscountType = DiscountType.Percentage,
            Value = 10,
            EndDate = DateTime.Now.AddDays(1),
            Name = "Pedro-Promo"
        };

        var result = _service.ApplyDiscount(100m, discount);
        Assert.Equal(90m, result);
    }

    [Fact]
    public void GetOrderTotal_CorrectSumOfQuantities()
    {
        var user = new AppUser
        {
            Id = "Pedro-User",
            FirstName = "Pedro",
            LastName = "Test", 
            Balance = 100m,
            BirthDate = DateTime.Now.AddYears(-20), 
            Gender = Gender.Male, 
            UserCategory = new UserCategory { Name = "Pedro-Student" }
        };

        var product = new Product
        {
            Id = 1,
            Name = "Pedro-Item",
            Price = 25m,
            Stock = 10,
            MinimumStock = 1,
            Description = "D",
            Category = new ProductCategory { Name = "Bar", Description = "D" }
        };

        var cart = new Order
        {
            TotalValue = 50m,
            AppUser = user,
            RedemptionCode = "PEDRO-TEST" 
        };

        cart.ProductPurchases.Add(new OrderLine
        {
            Quantity = 2,
            ProductId = 1,
            Product = product,
            OrderId = 0,
            Order = cart,
            ProductValue = 25m
        });

        cart.ProductPurchases.Add(new OrderLine
        {
            Quantity = 3,
            ProductId = 1,
            Product = product,
            OrderId = 0,
            Order = cart,
            ProductValue = 25m
        });

        var result = _service.GetOrderTotal(cart);

        Assert.Equal(5, result.TotalQuantity);
        Assert.Equal(50m, result.TotalValue);
    }
}
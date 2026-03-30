using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using Projeto_SEGUES;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace SeguesTests.RegressionTests.Orders;

public class ActiveOrderRegressionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public ActiveOrderRegressionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("RegressionDb_ActiveOrders_Pedro")
                        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (emailDescriptor != null) services.Remove(emailDescriptor);

                services.AddTransient<IEmailSender, MockHelper.FakeEmailSender>();

                var antiforgeryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAntiforgery));
                if (antiforgeryDescriptor != null) services.Remove(antiforgeryDescriptor);
                services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                        options.DefaultScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

        SeedDatabase().GetAwaiter().GetResult();
    }

    private async Task SeedDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var cat = new UserCategory { Name = "Estudante" };
        var pedro = new AppUser
        {
            Id = "pedro-77",
            UserName = "Pedro",
            Email = "pedro@test.com",
            FirstName = "Pedro",
            LastName = "Estudante",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = Gender.Male,
            UserCategory = cat
        };

        var prodCategory = new ProductCategory { Name = "Bebidas", Description = "Liquidos" };
        var product = new Product
        {
            Name = "Sumo",
            Description = "Sumo de Laranja",
            Price = 1.50m,
            Category = prodCategory,
            Stock = 50,
            MinimumStock = 5
        };

        var order = new Order
        {
            Id = 1,
            AppUser = pedro,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.Now
        };

        var orderLine = new OrderLine
        {
            Order = order,
            ProductId = product.Id,
            OrderId = order.Id,
            Product = product,
            Quantity = 1,
            ProductValue = 1.50m
        };

        order.ProductPurchases = new List<OrderLine> { orderLine };

        context.UserCategory.Add(cat);
        context.Users.Add(pedro);
        context.ProductCategory.Add(prodCategory);
        context.Product.Add(product);
        context.Order.Add(order);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Index_ReturnsCorrectStatusCode_DoesNotRegress()
    {
        var response = await _client.GetAsync("/Order/ActiveOrder/Index");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OrderDetails_MissingOrder_ReturnsRedirect_DoesNotRegress()
    {
        var response = await _client.GetAsync("/Order/ActiveOrder/OrderDetails/999");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("ActiveOrder", response.Headers.Location?.OriginalString ?? "");
    }
}
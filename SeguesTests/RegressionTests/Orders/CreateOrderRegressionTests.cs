using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using Projeto_SEGUES;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace SeguesTests.RegressionTests.Orders;

public class CreateOrderRegressionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CreateOrderRegressionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("RegressionDb_CreateOrder_Pedro")
                        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (emailDescriptor != null) services.Remove(emailDescriptor);

                services.AddTransient<IEmailSender, MockHelper.FakeEmailSender>();

                var mockAdmin = new Mock<IAdminService>();
                mockAdmin.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                mockAdmin.Setup(s => s.GetScheduleAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                mockAdmin.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
                services.AddSingleton(mockAdmin.Object);

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
            UserName = "pedro@test.com",
            Email = "pedro@test.com",
            FirstName = "Pedro",
            LastName = "Regressao",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = Gender.Male,
            UserCategory = cat,
            Balance = 10.00m
        };

        var prodCategory = new ProductCategory { Name = "Snacks", Description = "Comida" };
        var product = new Product
        {
            Id = 1,
            Name = "Batatas",
            Description = "Comestivel",
            Price = 1.00m,
            Category = prodCategory,
            Stock = 20,
            MinimumStock = 5
        };

        var cart = new Order
        {
            Id = 1,
            AppUser = pedro,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.Now
        };

        var line = new OrderLine
        {
            Order = cart,          
            OrderId = cart.Id,     
            Product = product,    
            ProductId = product.Id,
            Quantity = 2,
            ProductValue = 1.00m
        };

        cart.ProductPurchases = new List<OrderLine> { line };

        context.UserCategory.Add(cat);
        context.Users.Add(pedro);
        context.ProductCategory.Add(prodCategory);
        context.Product.Add(product);
        context.Order.Add(cart);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Checkout_Get_ReturnsOk_DoesNotRegress()
    {
        var response = await _client.GetAsync("/Order/CreateOrder/Checkout");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddToCart_InvalidProduct_ReturnsNotFound_DoesNotRegress()
    {
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "id", "999" },
            { "qty", "1" }
        });

        var response = await _client.PostAsync("/Order/CreateOrder/AddToCart", formContent);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System.Net;
using System.Net.Http.Headers;

namespace SeguesTests.IntegrationTests.Orders;

public class OrderManagementIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;

    public OrderManagementIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _dbName = "TestDb_" + Guid.NewGuid();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var dbDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext)).ToList();
                foreach (var d in dbDescriptors) services.Remove(d);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));

                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (emailDescriptor != null) services.Remove(emailDescriptor);

                services.AddTransient<IEmailSender, MockHelper.FakeEmailSender>();

                services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            });
        });
    }

    private async Task SeedDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;

        await using var db = new AppDbContext(options);

        db.Users.RemoveRange(db.Users);
        db.Order.RemoveRange(db.Order);
        db.UserCategory.RemoveRange(db.UserCategory);
        await db.SaveChangesAsync();

        var category = new UserCategory { Name = "Student" };
        db.UserCategory.Add(category);

        var staff = new AppUser
        {
            Id = "pedro-77",
            UserName = "Pedro",
            FirstName = "Pedro",
            LastName = "Jesus",
            Email = "pedro@segues.pt",
            BirthDate = new DateTime(2000, 1, 1),
            Balance = 0m,
            Gender = Gender.Male,
            UserCategory = category,
            NormalizedUserName = "PEDRO",
            NormalizedEmail = "PEDRO@SEGUES.PT",
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var customer = new AppUser
        {
            Id = "customer-id",
            UserName = "cliente_teste",
            FirstName = "Cliente",
            LastName = "Teste",
            Email = "cliente@teste.com",
            BirthDate = new DateTime(2000, 1, 1),
            Balance = 50m,
            Gender = Gender.Other,
            UserCategory = category,
            NormalizedUserName = "CLIENTE_TESTE",
            NormalizedEmail = "CLIENTE@TESTE.COM",
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var order = new Order
        {
            AppUser = customer,
            Status = OrderStatus.Pending,
            TotalValue = 10.50m,
            RedemptionCode = "PEDRO77",
            OrderDate = DateTime.Now,
            PickupTime = TimeSpan.Zero
        };

        db.Users.Add(staff);
        db.Users.Add(customer);
        db.Order.Add(order);
        await db.SaveChangesAsync();
    }

    private AppDbContext GetDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetOrdersTable_ReturnsHtmlContent_WithSeededOrder()
    {
        await SeedDatabase();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Admin");

        var response = await client.GetAsync("/Order/OrderManagement/GetOrdersTable");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cliente Teste", content);   
        Assert.Contains("cliente_teste", content);   
    }

    [Fact]
    public async Task UpdateStatus_ShouldPersistChangeInDatabase()
    {
        await SeedDatabase();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Admin");

        await using var seedDb = GetDb();
        var orderId = seedDb.Order.First().Id;

        var response = await client.PostAsync($"/Order/OrderManagement/UpdateStatus/{orderId}?newStatus=2", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var verifyDb = GetDb();
        var order = await verifyDb.Order.FirstAsync();
        Assert.Equal(OrderStatus.Preparing, order.Status);
    }

    [Fact]
    public async Task ValidateOrderCode_ShouldMarkAsDelivered_WhenCodeIsCorrect()
    {
        await SeedDatabase();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Employee");

        await using var seedDb = GetDb();
        var orderId = seedDb.Order.First().Id;

        var response = await client.PostAsync($"/Order/OrderManagement/ValidateOrderCode/{orderId}?enteredCode=PEDRO77", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var verifyDb = GetDb();
        var order = await verifyDb.Order.FirstAsync();
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }
}
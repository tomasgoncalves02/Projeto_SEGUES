using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System.Net.Http.Headers;
using Projeto_SEGUES;

namespace SeguesTests.RegressionTests.Report;

public class ReportOrderRegressionTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly AppDbContext _sharedDb;
    private readonly SqliteConnection _connection;

    public ReportOrderRegressionTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _sharedDb = new AppDbContext(dbOptions);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext)).ToList();

                foreach (var d in descriptors) services.Remove(d);

                services.AddSingleton(_sharedDb);

                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                        options.DefaultScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            });
        });
    }

    [Fact]
    public async Task GetOrderDetails_DeniesAccess_ToOtherUsersOrders()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var cat = new UserCategory { Name = "Estudante" };
        var stranger = new AppUser
        {
            Id = "stranger-id",
            FirstName = "Joao",
            LastName = "Silva",
            UserName = "joao",
            Email = "joao@teste.pt",
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = cat
        };

        var order = new Order
        {
            Id = 99,
            AppUser = stranger,
            RedemptionCode = "SECRET123",
            OrderDate = DateTime.Now
        };

        _sharedDb.Users.Add(stranger);
        _sharedDb.Order.Add(order);
        await _sharedDb.SaveChangesAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

        var response = await client.GetAsync("/Report/ReportOrder/GetOrderDetails/99");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("pedido solicitado", content);
        Assert.Contains("permiss", content); 
        Assert.DoesNotContain("SECRET123", content);
    }

    public void Dispose()
    {
        _sharedDb.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}
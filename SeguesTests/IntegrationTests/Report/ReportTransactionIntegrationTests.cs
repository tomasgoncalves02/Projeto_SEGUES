using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.IntegrationTests.Report
{
    public class ReportTransactionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly SqliteConnection _connection;

        public ReportTransactionIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType == typeof(AppDbContext)).ToList();
                    foreach (var d in descriptors) services.Remove(d);

                    services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                        options.DefaultScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        [Fact]
        public async Task Index_ShowsPedroTransactions()
        {
            const string userId = "pedro-77";

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.ChangeTracker.Clear();

                var category = new UserCategory { Name = "Estudante" };
                db.UserCategory.Add(category);

                var pedro = new AppUser
                {
                    Id = userId,
                    UserName = "pedro@test.com",
                    FirstName = "Pedro",
                    LastName = "Jesus",
                    BirthDate = new DateTime(2000, 1, 1),
                    Gender = Gender.Male,
                    UserCategory = category,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                db.Users.Add(pedro);
                db.Transaction.Add(new Transaction
                {
                    User = pedro,
                    Amount = 50.0m,
                    Reference = "RECARGA-PEDRO",
                    CreatedAt = DateTime.Now
                });

                await db.SaveChangesAsync();
            }

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", userId);

            var response = await client.GetAsync("/Report/ReportTransaction/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("RECARGA-PEDRO", content);
        }

        public void Dispose() => _connection.Dispose();
    }
}
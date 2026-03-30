using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using SeguesTests.Helpers;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.RegressionTests.Report
{
    public class ReportStatisticsOrderRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ReportStatisticsOrderRegressionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (dbDescriptor != null) services.Remove(dbDescriptor);

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("RegressionReportStatsOrderFinalDb_Pedro");
                    });

                    var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                    if (emailDescriptor != null) services.Remove(emailDescriptor);

                    services.AddTransient<IEmailSender, MockHelper.FakeEmailSender>();

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
        public async Task GetOrdersStats_ReturnsExpectedJsonStructure()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Admin");

            var response = await client.GetAsync("/Report/ReportStatisticsOrder/GetOrdersStats");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("totalOrders", content);
            Assert.Contains("totalRevenue", content);
            Assert.Contains("orderChart", content);
        }
    }
}
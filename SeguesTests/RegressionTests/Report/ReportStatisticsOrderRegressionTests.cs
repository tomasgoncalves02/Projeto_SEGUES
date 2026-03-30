using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SeguesTests.Helpers;
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
                builder.ConfigureServices(services =>
                {
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

            Assert.Contains("totalOrders", content);
            Assert.Contains("totalRevenue", content);
            Assert.Contains("orderChart", content);
        }
    }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES;
using SeguesTests.Helpers;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.SecurityTests.Report
{
    public class ReportStatisticsOrderSecurityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ReportStatisticsOrderSecurityTests(WebApplicationFactory<Program> factory)
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
                        options.UseInMemoryDatabase("SecurityReportStatsOrderFinalDb_Pedro");
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
        public async Task GetOrdersStats_ReturnsUnauthorized_ForAnonymousUser()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync("/Report/ReportStatisticsOrder/GetOrdersStats");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOrdersStats_ReturnsForbidden_ForRegularUser()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.GetAsync("/Report/ReportStatisticsOrder/GetOrdersStats");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetOrdersStats_ReturnsSuccess_ForAdminUser()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Admin");

            var response = await client.GetAsync("/Report/ReportStatisticsOrder/GetOrdersStats");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using SeguesTests.Helpers;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.IntegrationTests.Report
{
    public class ReportStatisticsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ReportStatisticsIntegrationTests(WebApplicationFactory<Program> factory)
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
                        options.UseInMemoryDatabase("ReportStatisticsIntegrationDb_Pedro");
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
        public async Task Index_ReturnsSuccess_ForAdminUser()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Admin");

            var response = await client.GetAsync("/Report/ReportStatistics/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Index_ReturnsForbidden_ForRegularUser()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.GetAsync("/Report/ReportStatistics/Index");

            Assert.True(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Redirect);
        }

        [Fact]
        public async Task Index_RedirectsToLogin_WhenAnonymous()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync("/Report/ReportStatistics/Index");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
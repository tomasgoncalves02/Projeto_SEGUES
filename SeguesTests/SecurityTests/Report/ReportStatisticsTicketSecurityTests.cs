using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SeguesTests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.SecurityTests.Report
{
    public class ReportStatisticsTicketSecurityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ReportStatisticsTicketSecurityTests(WebApplicationFactory<Program> factory)
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
        public async Task Index_ReturnsUnauthorized_ForAnonymousUser()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync("/Report/ReportStatisticsTicket/Index");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Index_ReturnsForbidden_ForRegularUser()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.GetAsync("/Report/ReportStatisticsTicket/Index");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTicketsStats_ReturnsUnauthorized_ForAnonymousUser()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync("/Report/ReportStatisticsTicket/GetTicketsStats");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTicketsStats_ReturnsForbidden_ForRegularUser()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.GetAsync("/Report/ReportStatisticsTicket/GetTicketsStats");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTicketsStats_ReturnsSuccess_ForAdminUser()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Admin");

            var response = await client.GetAsync("/Report/ReportStatisticsTicket/GetTicketsStats");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
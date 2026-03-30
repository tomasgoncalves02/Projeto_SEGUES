using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SeguesTests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.IntegrationTests.Report
{
    public class ReportIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ReportIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });
        }

        [Fact]
        public async Task Index_ReturnsSuccess_ForAuthenticatedPedro()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.GetAsync("/Report/Report/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
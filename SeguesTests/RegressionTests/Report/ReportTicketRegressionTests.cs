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
    public class ReportTicketRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ReportTicketRegressionTests(WebApplicationFactory<Program> factory)
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
                        options.UseInMemoryDatabase("RegressionReportTicketDb_Pedro_Fixed");
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
        public async Task GetFilteredHistory_ReturnsExpectedPartialViewStructure()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            var response = await client.GetAsync("/Report/ReportTicket/GetFilteredHistory?SearchString=Pedro");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.DoesNotContain("<!DOCTYPE html>", content);
        }
    }
}
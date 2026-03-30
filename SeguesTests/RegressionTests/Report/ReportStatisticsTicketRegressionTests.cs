using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using SeguesTests.Helpers;
using System.Net.Http.Headers;
using Projeto_SEGUES;

namespace SeguesTests.RegressionTests.Report;

public class ReportStatisticsTicketRegressionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReportStatisticsTicketRegressionTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("RegressionReportStatsTicketFinalDb_Pedro");
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

                services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
                {
                    options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
                });
            });
        });
    }

    [Fact]
    public async Task GetTicketsStats_Structure_RemainsConsistent()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Admin");

        var response = await client.GetAsync("/Report/ReportStatisticsTicket/GetTicketsStats");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        Assert.Contains("totalUsedTickets", content);
        Assert.Contains("totalRevenue", content);
        Assert.Contains("chart", content);
        Assert.Contains("byCategory", content);
    }

    [Fact]
    public async Task Index_View_ContainsRequiredDashboardElements()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Admin");

        var response = await client.GetAsync("/Report/ReportStatisticsTicket/Index");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("tatisticas", content);
        Assert.Contains("enhas", content);
    }
}
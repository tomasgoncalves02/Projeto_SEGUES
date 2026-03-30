using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES;
using SeguesTests.Helpers;
using System.Net;

namespace SeguesTests.SecurityTests.Report;

public class ReportOrderSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReportOrderSecurityTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("SecurityReportTestDb_Pedro");
                });

                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (emailDescriptor != null) services.Remove(emailDescriptor);

                services.AddTransient<IEmailSender, MockHelper.FakeEmailSender>();
            });
        });
    }

    [Theory]
    [InlineData("/Report/ReportOrder/Index")]
    [InlineData("/Report/ReportOrder/GetOrderDetails/1")]
    public async Task Endpoints_RedirectToRoot_WhenAnonymous(string url)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);

        var uri = new Uri(location, UriKind.RelativeOrAbsolute);
        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : location.Split('?')[0];

        Assert.Equal("/", path);
    }
}
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

public class ReportSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReportSecurityTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("SecurityReportIndexTestDb_Pedro");
                });

                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (emailDescriptor != null) services.Remove(emailDescriptor);

                services.AddTransient<IEmailSender, MockHelper.FakeEmailSender>();
            });
        });
    }

    [Fact]
    public async Task Index_RedirectsToRoot_WhenAnonymous()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Report/Report/Index");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        var uri = new Uri(location, UriKind.RelativeOrAbsolute);
        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : location.Split('?')[0];

        Assert.Equal("/", path);
    }
}
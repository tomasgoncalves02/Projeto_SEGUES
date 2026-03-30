using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using Projeto_SEGUES;

namespace SeguesTests.RegressionTests.Home;

public class HomeControllerRegressionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HomeControllerRegressionTests(WebApplicationFactory<Program> factory)
    {
        var factory1 = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("HomeRegDb"));

                var adminDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAdminService));
                if (adminDescriptor != null) services.Remove(adminDescriptor);

                var mockAdmin = new Mock<IAdminService>();
                mockAdmin.Setup(s => s.GetMenuLinksAsync())
                    .ReturnsAsync(new BarCanteenConfigViewModel { CanteenMenuLink = "/canteen", BarMenuLink = "/bar" });
                services.AddSingleton(mockAdmin.Object);
            });
        });

        _client = factory1.CreateClient();
    }

    [Fact]
    public async Task Error_Default_ReturnsSuccessStatusCodeAndHtml()
    {
        var response = await _client.GetAsync("/Home/Error");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task Error_WithSpecificCode_ReturnsSuccessStatusCodeAndHtml()
    {
        const int errorCode = (int)AppErrors.UserNotFound;
        var response = await _client.GetAsync($"/Home/Error?errorCode={errorCode}");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }
}
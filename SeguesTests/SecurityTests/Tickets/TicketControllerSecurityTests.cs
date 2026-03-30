using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Net;
using System.Reflection;
using Projeto_SEGUES;
using Projeto_SEGUES.Areas.Ticket.Controllers;

namespace SeguesTests.SecurityTests.Tickets;

public class TicketControllerSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TicketControllerSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("SecurityDb_Tickets_Final"));

                var adminDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAdminService));
                if (adminDescriptor != null) services.Remove(adminDescriptor);
                var mockAdmin = new Mock<IAdminService>();
                mockAdmin.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                services.AddSingleton(mockAdmin.Object);

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            });
        });
    }

    [Fact]
    public void Controller_MustHave_AuthorizeAttribute()
    {
        var attribute = typeof(TicketController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Controller_MustHave_AreaTicketAttribute()
    {
        var attribute = typeof(TicketController).GetCustomAttribute<AreaAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal("Ticket", attribute.RouteValue);
    }

    [Fact]
    public void TransferTickets_Post_MustHave_ValidateAntiForgeryToken()
    {
        var method = typeof(TicketController).GetMethod("TransferTickets");
        var attribute = method?.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();
        Assert.NotNull(attribute);
    }

    [Theory]
    [InlineData("/Ticket/Ticket/Index")]
    [InlineData("/Ticket/Ticket/ActiveTickets")]
    [InlineData("/Ticket/Ticket/TransferTicket")]
    public async Task Get_Endpoints_ReturnRedirectToLogin_WhenUnauthenticated(string url)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString() ?? "";

        Assert.Contains("ReturnUrl", location);
    }

    [Fact]
    public async Task TransferTickets_Post_ReturnsRedirectToLogin_WhenUnauthenticated()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/Ticket/Ticket/TransferTickets", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
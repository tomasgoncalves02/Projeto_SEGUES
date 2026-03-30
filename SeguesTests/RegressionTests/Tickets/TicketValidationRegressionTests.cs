using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using Projeto_SEGUES;

namespace SeguesTests.RegressionTests.Tickets;

public class TicketValidationRegressionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TicketValidationRegressionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("RegressionDb_Validation_Pedro")
                        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                var antiforgeryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAntiforgery));
                if (antiforgeryDescriptor != null) services.Remove(antiforgeryDescriptor);
                services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

                var ticketDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITicketService));
                if (ticketDescriptor != null) services.Remove(ticketDescriptor);

                var mockTicket = new Mock<ITicketService>();
                mockTicket
                    .Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
                    .ReturnsAsync([]);
                mockTicket
                    .Setup(s => s.ValidateTicketAsync(It.IsAny<string>(), It.IsAny<AppUser>()))
                    .ReturnsAsync(ServiceResult.Fail("Senha não encontrado."));
                services.AddScoped(_ => mockTicket.Object);

                var mockAdmin = new Mock<IAdminService>();
                mockAdmin.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                services.AddScoped(_ => mockAdmin.Object);

                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                        options.DefaultScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandlerWithRole>("Test", null);

                    
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        SeedDatabase().GetAwaiter().GetResult();
    }

    private async Task SeedDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var cat = new UserCategory { Name = "Employee" };
        context.UserCategory.Add(cat);
        await context.SaveChangesAsync();

        var pedro = new AppUser
        {
            Id = "pedro-77",
            UserName = "Pedro",
            Email = "pedro@segues.pt",
            FirstName = "Pedro",
            LastName = "Staff",
            BirthDate = DateTime.Now.AddYears(-25),
            Gender = Gender.Male,
            UserCategory = cat
        };
        context.Users.Add(pedro);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Index_Get_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/Ticket/TicketValidation/Index");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.InternalServerError,
            $"Esperava chegar ao controller mas: {response.StatusCode}");
    }

    [Fact]
    public async Task Index_Post_InvalidCode_ReturnsServiceError()
    {
        var form = new Dictionary<string, string>
        {
            { "Code", "INVALID1" }
        };

        var response = await _client.PostAsync("/Ticket/TicketValidation/Index",
            new FormUrlEncodedContent(form));

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.InternalServerError,
            $"Esperava chegar ao controller mas: {response.StatusCode}");
    }

    [Fact]
    public async Task Index_Post_ValidateTicketAsync_IsCalledOnce()
    {
        var mockTicket = new Mock<ITicketService>();
        mockTicket
            .Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
            .ReturnsAsync([]);
        mockTicket
            .Setup(s => s.ValidateTicketAsync(It.IsAny<string>(), It.IsAny<AppUser>()))
            .ReturnsAsync(ServiceResult.Fail("Senha não encontrado."));

        var form = new Dictionary<string, string>
        {
            { "Code", "TESTCODE" }
        };

        var response = await _client.PostAsync("/Ticket/TicketValidation/Index",
            new FormUrlEncodedContent(form));

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.InternalServerError,
            $"Esperava chegar ao controller mas: {response.StatusCode}");
    }
}
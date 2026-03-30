using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using Projeto_SEGUES;

namespace SeguesTests.IntegrationTests.Tickets;

public class TicketValidationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TicketValidationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("IntegDb_Validation_Pedro")
                        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                var antiforgeryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAntiforgery));
                if (antiforgeryDescriptor != null) services.Remove(antiforgeryDescriptor);
                services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

                var mockAdmin = new Mock<IAdminService>();
                mockAdmin.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                mockAdmin.Setup(s => s.GetScheduleAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
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

        SeedDatabase("TKT-1234").GetAwaiter().GetResult();
    }

    private async Task SeedDatabase(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var cat = new UserCategory { Name = "Staff" };
        var pedro = new AppUser
        {
            Id = "pedro-77",
            UserName = "Pedro",
            Email = "pedro@test.com",
            FirstName = "Pedro",
            LastName = "Staff",
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = cat
        };

        var purchase = new TicketPurchase
        {
            AppUser = pedro,
            Quantity = 1,
            TransactionDate = DateTime.Now,
            Value = 2.50m
        };

        context.Ticket.Add(new Ticket
        {
            Id = 1,
            Owner = pedro,
            TicketPurchase = purchase,
            ValidationCode = code,
            IsUsed = false,
            State = TicketState.Available,
            ExpirationDate = DateTime.Now.AddDays(7)
        });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Index_Post_ValidCode_UpdatesDatabaseToUsed()
    {
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Code", "TKT-1234" }
        });

        var response = await _client.PostAsync("/Ticket/TicketValidation/Index", formContent);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ticketInDb = await context.Ticket
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ValidationCode == "TKT-1234");

        Assert.NotNull(ticketInDb);
        Assert.True(ticketInDb.IsUsed, "O ticket deveria estar marcado como USADO na DB.");
        Assert.Equal(TicketState.Used, ticketInDb.State);
    }
}
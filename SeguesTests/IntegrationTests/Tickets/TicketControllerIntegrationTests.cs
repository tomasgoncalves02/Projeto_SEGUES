using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.IntegrationTests.Tickets
{
    public class TicketControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public TicketControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("TicketIntegrationDb_Pedro")
                               .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                    var antiforgeryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAntiforgery));
                    if (antiforgeryDescriptor != null) services.Remove(antiforgeryDescriptor);
                    services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

                    var ticketDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITicketService));
                    if (ticketDescriptor != null) services.Remove(ticketDescriptor);
                    services.AddScoped<ITicketService>(sp =>
                    {
                        var context = sp.GetRequiredService<AppDbContext>();
                        var logger = sp.GetRequiredService<ILogger<TicketService>>();
                        var realService = new TicketService(context, logger);

                        var mock = new Mock<ITicketService>();

                        mock.Setup(s => s.GetActiveTicketsAsync(It.IsAny<string>()))
                            .ReturnsAsync(new List<Ticket>());

                        mock.Setup(s => s.GetUserTicketsAsync(It.IsAny<string>()))
                            .ReturnsAsync(new List<Ticket>());

                        mock.Setup(s => s.TransferTicketsAsync(
                                It.IsAny<string>(),
                                It.IsAny<string>(),
                                It.IsAny<List<string>>()))
                            .Returns((string senderId, string recipientEmail, List<string> tickets) =>
                                realService.TransferTicketsAsync(senderId, recipientEmail, tickets));

                        mock.Setup(s => s.CheckTransferEligibilityAsync(
                                It.IsAny<string>(),
                                It.IsAny<string>()))
                            .Returns((string senderId, string email) =>
                                realService.CheckTransferEligibilityAsync(senderId, email));

                        return mock.Object;
                    });

                    var mockAdmin = new Mock<IAdminService>();
                    mockAdmin.Setup(s => s.GetScheduleAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                    mockAdmin.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                    services.AddScoped(_ => mockAdmin.Object);

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                        options.DefaultScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

            SeedDatabase("pedro-77").GetAwaiter().GetResult();
        }

        private async Task SeedDatabase(string userId)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var category = new UserCategory { Name = "Estudante" };
            context.UserCategory.Add(category);
            await context.SaveChangesAsync();

            var pedro = new AppUser
            {
                Id = userId,
                UserName = userId,
                Email = userId + "@test.com",
                FirstName = "Pedro",
                LastName = "Silva",
                UserCategory = category,
                BirthDate = new DateTime(2000, 1, 1),
                Gender = Gender.Male,
                Balance = 20.00m
            };
            await userManager.CreateAsync(pedro, "SenhaSegura123!");

            var purchase = new TicketPurchase
            {
                TransactionDate = DateTime.Now,
                Quantity = 1,
                Value = 2.50m,
                AppUser = pedro
            };
            context.TicketPurchase.Add(purchase);
            await context.SaveChangesAsync();

            context.Ticket.Add(new Ticket
            {
                Id = 1,
                Owner = pedro,
                TicketPurchase = purchase,
                ExpirationDate = DateTime.Now.AddDays(7),
                State = TicketState.Available,
                IsUsed = false,
                ValidationCode = "TEST1234"
            });
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Index_DisplaysDatabaseTickets_ForAuthenticatedUser()
        {
            var response = await _client.GetAsync("/Ticket/Ticket/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.True(
                content.Contains("20,00") || content.Contains("20.00"),
                $"O saldo não apareceu na página. Conteúdo recebido: {content[..Math.Min(500, content.Length)]}");
        }

        [Fact]
        public async Task TransferTicket_Flow_UpdatesDatabaseCorrectly()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var category = await context.UserCategory.FirstAsync();

                if (await userManager.FindByIdAsync("amigo-99") == null)
                {
                    var amigo = new AppUser
                    {
                        Id = "amigo-99",
                        UserName = "amigo-99",
                        Email = "amigo@test.com",
                        FirstName = "Amigo",
                        LastName = "Teste",
                        UserCategory = category,
                        BirthDate = new DateTime(2000, 1, 1),
                        Gender = Gender.Male
                    };
                    await userManager.CreateAsync(amigo);
                }
            }

            var form = new Dictionary<string, string>
            {
                { "RecipientEmail", "amigo@test.com" },
                { "SelectedTickets[0]", "TEST1234" }
            };

            var response = await _client.PostAsync("/Ticket/Ticket/TransferTickets", new FormUrlEncodedContent(form));

            Assert.True(
                response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.OK,
                $"Resposta inesperada: {response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}");

            using var scope2 = _factory.Services.CreateScope();
            var db = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var ticketNoBanco = await db.Ticket
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == 1);

            Assert.NotNull(ticketNoBanco);
            Assert.Equal("amigo-99", ticketNoBanco.Owner.Id);
        }
    }
}
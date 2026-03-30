using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Ticket;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.RegressionTests.Tickets
{
    public class TicketControllerRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public TicketControllerRegressionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("TicketRegDb_Final_Pedro"));

                    var employeeUser = MockHelper.CreateValidAppUser("employee-1");
                    employeeUser.Email = "employee@employee.com";
                    employeeUser.UserName = "employee@employee.com";

                    var adminUser = MockHelper.CreateValidAppUser("admin-1");
                    adminUser.Email = "admin@admin.com";
                    adminUser.UserName = "admin@admin.com";

                    var pedroUser = MockHelper.CreateValidAppUser("pedro-77");

                    var userDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(UserManager<AppUser>));
                    if (userDescriptor != null) services.Remove(userDescriptor);

                    var mockUserManager = MockHelper.MockUserManager(new List<AppUser>());

                    mockUserManager
                        .Setup(m => m.Users)
                        .Returns(new List<AppUser>().AsQueryable());

                    mockUserManager
                        .Setup(m => m.GetUsersInRoleAsync(It.IsAny<string>()))
                        .ReturnsAsync(new List<AppUser>());

                    mockUserManager
                        .Setup(m => m.FindByEmailAsync("employee@employee.com"))
                        .ReturnsAsync(employeeUser);

                    mockUserManager
                        .Setup(m => m.FindByEmailAsync("admin@admin.com"))
                        .ReturnsAsync(adminUser);

                    mockUserManager
                        .Setup(m => m.FindByEmailAsync(It.Is<string>(e =>
                            e != "employee@employee.com" && e != "admin@admin.com")))
                        .ReturnsAsync((AppUser?)null);

                    mockUserManager
                        .Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(pedroUser);

                    services.AddScoped(_ => mockUserManager.Object);

                    var roleDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(RoleManager<Role>));
                    if (roleDescriptor != null) services.Remove(roleDescriptor);

                    var mockRoleManager = MockHelper.MockRoleManager<Role>();
                    mockRoleManager
                        .Setup(m => m.Roles)
                        .Returns(new List<Role>().AsQueryable());

                    services.AddScoped(_ => mockRoleManager.Object);

                    var ticketServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITicketService));
                    if (ticketServiceDescriptor != null) services.Remove(ticketServiceDescriptor);

                    var mockTicketService = new Mock<ITicketService>();

                    mockTicketService
                        .Setup(s => s.CheckTransferEligibilityAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(ServiceResult<string>.Fail("Utilizador não encontrado"));

                    mockTicketService
                        .Setup(s => s.GetUserTicketsAsync(It.IsAny<string>()))
                        .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

                    mockTicketService
                        .Setup(s => s.GetActiveTicketsAsync(It.IsAny<string>()))
                        .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

                    services.AddScoped(_ => mockTicketService.Object);

                    var adminDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAdminService));
                    if (adminDescriptor != null) services.Remove(adminDescriptor);

                    var mockAdmin = new Mock<IAdminService>();
                    mockAdmin
                        .Setup(s => s.GetMenuLinksAsync())
                        .ReturnsAsync(new BarCanteenConfigViewModel());
                    services.AddScoped(_ => mockAdmin.Object);

                    var antiforgeryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAntiforgery));
                    if (antiforgeryDescriptor != null) services.Remove(antiforgeryDescriptor);
                    services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });
        }

        [Fact]
        public async Task CheckTransferEligibility_MissingEmail_ReturnsFailureJson()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

            var response = await client.GetAsync("/Ticket/Ticket/CheckTransferEligibility?email=");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"success\":false", content);
        }

        [Fact]
        public async Task GetUpdatedTickets_ReturnsPartialViewContent()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

            var response = await client.GetAsync("/Ticket/Ticket/GetUpdatedTickets");

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("<tr>", content);
            Assert.Contains("<td", content);
        }
    }
}
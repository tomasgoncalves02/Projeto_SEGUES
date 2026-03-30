using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.SecurityTests.Tickets
{
    public class TicketValidationSecurityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public TicketValidationSecurityTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("SecurityDb_Validation_Final_Fixed"));

                    var mockAdmin = new Mock<IAdminService>();
                    mockAdmin.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                    services.AddSingleton(mockAdmin.Object);

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });
        }

        private async Task SeedUserWithRole(IServiceProvider services, string userId, string roleName)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var category = await context.UserCategory.FirstOrDefaultAsync(c => c.Name == roleName);
            if (category == null)
            {
                category = new UserCategory { Name = roleName };
                context.UserCategory.Add(category);
                await context.SaveChangesAsync();
            }

            if (!context.Users.Any(u => u.Id == userId))
            {
                var user = new AppUser
                {
                    Id = userId,
                    UserName = userId,
                    Email = userId + "@segues.pt",
                    FirstName = "Pedro",
                    LastName = "Security",
                    BirthDate = DateTime.Now.AddYears(-20),
                    Gender = Gender.Male,
                    UserCategory = category
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task Index_Get_Student_ReturnsForbidden()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Student");

            await SeedUserWithRole(_factory.Services, "pedro-student", "Student");

            var response = await client.GetAsync("/Ticket/TicketValidation/Index");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        }

       

        [Fact]
        public async Task Index_Get_Employee_ReturnsSuccess()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Employee");

            await SeedUserWithRole(_factory.Services, "pedro-77", "Employee");

            var response = await client.GetAsync("/Ticket/TicketValidation/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Index_Post_MissingAntiforgery_ReturnsBadRequest()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "Employee");

            await SeedUserWithRole(_factory.Services, "pedro-77", "Employee");

            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "Code", "ANYCODE" }
            });

            var response = await client.PostAsync("/Ticket/TicketValidation/Index", formContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
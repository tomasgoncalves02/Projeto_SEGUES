using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
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

namespace SeguesTests.IntegrationTests.Payment
{
    public class PaymentControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public PaymentControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("PaymentIntDb_Definitive"));

                    var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                    if (emailDescriptor != null) services.Remove(emailDescriptor);

                    services.AddTransient<IEmailSender, MockHelper.FakeEmailSender>();

                    var adminDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAdminService));
                    if (adminDescriptor != null) services.Remove(adminDescriptor);
                    var mockAdmin = new Mock<IAdminService>();
                    mockAdmin.Setup(s => s.GetMenuLinksAsync())
                             .ReturnsAsync(new BarCanteenConfigViewModel { CanteenMenuLink = "/c", BarMenuLink = "/b" });
                    services.AddSingleton(mockAdmin.Object);

                    var payDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPaymentService));
                    if (payDescriptor != null) services.Remove(payDescriptor);
                    var mockPay = new Mock<IPaymentService>();
                    mockPay.Setup(s => s.CreateStripeSessionAsync(It.IsAny<AppUser>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                           .ReturnsAsync("https://checkout.stripe.com/test");
                    services.AddSingleton(mockPay.Object);

                    var antiforgeryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAntiforgery));
                    if (antiforgeryDescriptor != null) services.Remove(antiforgeryDescriptor);
                    var mockAnti = new Mock<IAntiforgery>();
                    mockAnti.Setup(x => x.GetAndStoreTokens(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
                            .Returns(new AntiforgeryTokenSet("t", "t", "t", "t"));
                    services.AddSingleton(mockAnti.Object);

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                        options.DefaultScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });
        }

        [Fact]
        public async Task CreateCheckoutSession_ValidPedroRequest_RedirectsToStripe()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                if (!db.Users.Any(u => u.UserName == "Pedro"))
                {
                    db.Users.Add(new Student
                    {
                        Id = "pedro-77",
                        UserName = "Pedro",
                        Email = "p@s.pt",
                        FirstName = "Pedro",
                        LastName = "J",
                        BirthDate = DateTime.Now.AddYears(-20),
                        Gender = Gender.Male,
                        UserCategory = new UserCategory { Name = "E" },
                        StudentNumber = "1"
                    });
                    await db.SaveChangesAsync();
                }
            }

            var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "Amount", "50" } });

            var response = await client.PostAsync("/Payment/Payment/CreateCheckoutSession", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"💥 Esperado Redirect, mas deu OK. Corpo da resposta: {body}");
            }

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("stripe.com", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task CancelPayment_RedirectsToHome()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

            var response = await client.GetAsync("/Payment/Payment/CancelPayment");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var location = response.Headers.Location?.ToString() ?? "";
            Assert.True(location == "/" || location.EndsWith("localhost/"), $"Redirect errado: {location}");
        }
    }
}
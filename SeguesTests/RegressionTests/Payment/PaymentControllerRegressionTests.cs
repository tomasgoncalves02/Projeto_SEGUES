using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Net;
using System.Net.Http.Headers;

namespace SeguesTests.RegressionTests.Payment;

public class PaymentControllerRegressionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaymentControllerRegressionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing"); 

            builder.ConfigureServices(services =>
            {
                // 1. Configurar Base de Dados em Memória
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("PayRegDb_Final_v2"));

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
                services.AddSingleton(new Mock<IPaymentService>().Object);

  
                var antiforgeryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAntiforgery));
                if (antiforgeryDescriptor != null) services.Remove(antiforgeryDescriptor);

                var mockAntiforgery = new Mock<IAntiforgery>();
                mockAntiforgery.Setup(x => x.GetAndStoreTokens(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
                    .Returns(new AntiforgeryTokenSet("test", "test", "test", "test"));
                services.AddSingleton(mockAntiforgery.Object);

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
    public async Task CreateCheckoutSession_InvalidAmount_ReturnsDepositView()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Amount", "0" }
        });

        var response = await client.PostAsync("/Payment/Payment/CreateCheckoutSession", content);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"BadRequest detetado! Corpo da resposta: {errorBody}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SuccessPayment_MissingParams_RedirectsToGlobalError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.GetAsync("/Payment/Payment/SuccessPayment?reference=&sessionId=");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.Contains("errorCode", location);
    }
}
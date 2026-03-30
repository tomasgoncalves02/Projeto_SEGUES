using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES;
using SeguesTests.Helpers;
using System.Net;

namespace SeguesTests.SecurityTests.Users;

public class UserControllerSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UserControllerSecurityTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("SecurityUserFinalTestDb_Pedro");
                });

                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (emailDescriptor != null) services.Remove(emailDescriptor);

                services.AddTransient<IEmailSender, MockHelper.FakeEmailSender>();
            });
        });
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Index_UnauthenticatedUser_ReturnsRedirectToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/User/User/Index");

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect
        );
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task UpdateProfile_MissingAntiForgeryToken_ReturnsBadRequestOrInternalError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var formData = new Dictionary<string, string>
        {
            { "FirstName", "Pedro" },
            { "Email", "pedro@evil.com" }
        };
        var content = new FormUrlEncodedContent(formData);

        var response = await client.PostAsync("/User/User/UpdateProfile", content);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [Trait("Category", "Security")]
    [InlineData("Pedro'; DROP TABLE Users; --")]
    [InlineData("Pedro' OR '1'='1")]
    [InlineData("Pedro'; WAITFOR DELAY '0:0:5'--")]
    public async Task UpdateProfile_SqlInjectionPayload_IsHandledSafely(string maliciousPayload)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var formData = new Dictionary<string, string>
        {
            { "FirstName", maliciousPayload },
            { "LastName", "Jesus" },
            { "Email", "pedro@segues.pt" }
        };
        var content = new FormUrlEncodedContent(formData);

        var response = await client.PostAsync("/User/User/UpdateProfile", content);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using SeguesTests.Helpers; // Onde está a tua CustomWebApplicationFactory
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.SecurityTests.Users;

// Alterado para usar CustomWebApplicationFactory para travar e-mails e injetar fakes
public class UserControllerSecurityTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public UserControllerSecurityTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Index_UnauthenticatedUser_ReturnsRedirectToLogin()
    {
        // Act
        var response = await _client.GetAsync("/User/User/Index");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found
        );

        if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString();
            Assert.Contains("ReturnUrl", location, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task UpdateProfile_MissingAntiForgeryToken_ReturnsBadRequestOrInternalError()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "FirstName", "Pedro" },
            { "Email", "pedro@evil.com" }
        };
        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/User/User/UpdateProfile", content);

        // Assert
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Theory]
    [Trait("Category", "Security")]
    [InlineData("Pedro'; DROP TABLE Users; --")]
    [InlineData("Pedro' OR '1'='1")]
    [InlineData("Pedro'; WAITFOR DELAY '0:0:5'--")]
    public async Task UpdateProfile_SqlInjectionPayload_IsHandledSafely(string maliciousPayload)
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "FirstName", maliciousPayload },
            { "LastName", "Jesus" },
            { "Email", "pedro@segues.pt" }
        };
        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/User/User/UpdateProfile", content);

        // Assert
        // Com o CustomWebApplicationFactory, se isto disparasse e-mail, seria bloqueado.
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
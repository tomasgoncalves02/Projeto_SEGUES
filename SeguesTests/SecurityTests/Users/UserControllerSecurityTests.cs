using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Tests.SecurityTests.User;

public class UserControllerSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UserControllerSecurityTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Index_UnauthenticatedUser_ReturnsRedirectToLogin()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/User/User/Index");

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
    public async Task UpdateProfile_MissingAntiForgeryToken_ReturnsBadRequestOrInternalError()
    {
        var formData = new Dictionary<string, string>
    {
        { "FirstName", "Pedro" },
        { "Email", "pedro@evil.com" }
    };
        var content = new FormUrlEncodedContent(formData);

        var response = await _client.PostAsync("/User/User/UpdateProfile", content);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Theory]
    [InlineData("Pedro'; DROP TABLE Users; --")]
    [InlineData("Pedro' OR '1'='1")]
    [InlineData("Pedro'; WAITFOR DELAY '0:0:5'--")]
    public async Task UpdateProfile_SqlInjectionPayload_IsHandledSafely(string maliciousPayload)
    {
        var formData = new Dictionary<string, string>
        {
            { "FirstName", maliciousPayload },
            { "LastName", "Jesus" },
            { "Email", "pedro@segues.pt" }
        };

        var content = new FormUrlEncodedContent(formData);

        var response = await _client.PostAsync("/User/User/UpdateProfile", content);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
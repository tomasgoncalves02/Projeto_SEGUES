using Microsoft.AspNetCore.Mvc.Testing;
using Projeto_SEGUES.Models.Enums;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.RegressionTests.Home
{
    public class HomeControllerRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public HomeControllerRegressionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Error_Default_ReturnsSuccessStatusCodeAndHtml()
        {
            var response = await _client.GetAsync("/Home/Error");

            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task Error_WithSpecificCode_ReturnsSuccessStatusCodeAndHtml()
        {
            var errorCode = (int)AppErrors.UserNotFound;
            var response = await _client.GetAsync($"/Home/Error?errorCode={errorCode}");

            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }
    }
}
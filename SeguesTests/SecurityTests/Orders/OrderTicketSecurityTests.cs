using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.SecurityTests.Orders
{
    public class OrderTicketSecurityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public OrderTicketSecurityTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/Order/OrderTicket/Index")]
        public async Task GetEndpoints_RedirectToRoot_WhenAnonymous(string url)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var location = response.Headers.Location?.ToString();
            Assert.NotNull(location);

            var uri = new Uri(location, UriKind.RelativeOrAbsolute);
            var path = uri.IsAbsoluteUri ? uri.AbsolutePath : location.Split('?')[0];

            Assert.Equal("/", path);
        }

        [Fact]
        public async Task BuyTicket_Post_RedirectsToRoot_WhenAnonymous()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.PostAsync("/Order/OrderTicket/BuyTicket", null);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var location = response.Headers.Location?.ToString();
            Assert.NotNull(location);

            var uri = new Uri(location, UriKind.RelativeOrAbsolute);
            var path = uri.IsAbsoluteUri ? uri.AbsolutePath : location.Split('?')[0];

            Assert.Equal("/", path);
        }
    }
}
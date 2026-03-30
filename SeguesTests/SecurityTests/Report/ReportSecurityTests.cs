using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.SecurityTests.Report
{
    public class ReportSecurityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ReportSecurityTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Index_RedirectsToRoot_WhenAnonymous()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync("/Report/Report/Index");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var location = response.Headers.Location?.ToString();
            Assert.NotNull(location);
            var uri = new Uri(location, UriKind.RelativeOrAbsolute);
            var path = uri.IsAbsoluteUri ? uri.AbsolutePath : location.Split('?')[0];

            Assert.Equal("/", path);
        }
    }
}
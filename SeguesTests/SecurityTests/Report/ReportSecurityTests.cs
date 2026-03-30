using Microsoft.AspNetCore.Mvc.Testing;
using Projeto_SEGUES;
using SeguesTests.Helpers;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.SecurityTests.Report
{
    public class ReportSecurityTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public ReportSecurityTests(CustomWebApplicationFactory<Program> factory)
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
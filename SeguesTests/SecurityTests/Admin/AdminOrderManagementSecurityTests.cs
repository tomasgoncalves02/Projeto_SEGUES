using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.Controllers;
using System.Reflection;
using Xunit;

namespace SeguesTests.SecurityTests.Admin
{
    public class AdminOrderManagementSecurityTests
    {
        [Fact]
        public void Controller_ShouldBeRestrictedToAdminRole()
        {
            var type = typeof(AdminOrderManagementController);
            var attribute = type.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal("Admin", attribute.Roles);
        }

        [Fact]
        public void Controller_ShouldSpecifyAdminArea()
        {
            var type = typeof(AdminOrderManagementController);
            var attribute = type.GetCustomAttribute<AreaAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal("Admin", attribute.RouteValue);
        }

        [Theory]
        [InlineData("UpdateOpenAndCloseTime")]
        [InlineData("UpdateWeekendStatus")]
        public void PostMethods_ShouldRequireAntiforgeryToken(string methodName)
        {
            var type = typeof(AdminOrderManagementController);
            var method = type.GetMethods()
                .FirstOrDefault(m => m.Name == methodName && m.GetCustomAttribute<HttpPostAttribute>() != null);

            var attribute = method?.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();

            Assert.NotNull(attribute);
        }
    }
}
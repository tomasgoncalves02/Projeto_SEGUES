using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.Controllers;
using System.Reflection;
using Xunit;

namespace SeguesTests.SecurityTests.Admin;

public class AdminUserManagementSecurityTests
{
    [Fact]
    public void Controller_RestrictedToAdminRole()
    {
        var type = typeof(AdminUserManagementController);
        var authAttribute = type.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authAttribute);
        Assert.Equal("Admin", authAttribute.Roles);
    }

    [Fact]
    public void Controller_InCorrectArea()
    {
        var type = typeof(AdminUserManagementController);
        var areaAttribute = type.GetCustomAttribute<AreaAttribute>();

        Assert.NotNull(areaAttribute);
        Assert.Equal("Admin", areaAttribute.RouteValue);
    }

    [Theory]
    [InlineData("Edit")]
    [InlineData("Deactivate")]
    [InlineData("Activate")]
    public void PostActions_RequireAntiforgeryToken(string methodName)
    {
        var method = typeof(AdminUserManagementController)
            .GetMethods()
            .FirstOrDefault(m => m.Name == methodName && m.GetCustomAttribute<HttpPostAttribute>() != null);

        Assert.NotNull(method?.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}
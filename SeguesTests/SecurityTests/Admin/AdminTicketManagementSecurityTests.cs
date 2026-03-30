using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.Controllers;
using System.Reflection;
using Xunit;

namespace SeguesTests.SecurityTests.Admin;

public class AdminTicketManagementSecurityTests
{
    [Fact]
    public void Controller_RestrictedToAdminRole()
    {
        var type = typeof(AdminTicketManagementController);
        var authAttribute = type.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authAttribute);
        Assert.Equal("Admin", authAttribute.Roles);
    }

    [Fact]
    public void Controller_InCorrectArea()
    {
        var type = typeof(AdminTicketManagementController);
        var areaAttribute = type.GetCustomAttribute<AreaAttribute>();

        Assert.NotNull(areaAttribute);
        Assert.Equal("Admin", areaAttribute.RouteValue);
    }

    [Theory]
    [InlineData("UpdateSchedule")]
    [InlineData("UpdatePrices")]
    [InlineData("UpdateValidity")]
    public void PostActions_RequireAntiforgeryToken(string methodName)
    {
        var method = typeof(AdminTicketManagementController)
            .GetMethods()
            .FirstOrDefault(m => m.Name == methodName && m.GetCustomAttribute<HttpPostAttribute>() != null);

        Assert.NotNull(method?.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}
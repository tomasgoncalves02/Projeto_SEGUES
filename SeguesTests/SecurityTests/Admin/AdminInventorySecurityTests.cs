using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using System.Reflection;

namespace SeguesTests.SecurityTests.Admin;

public class AdminInventorySecurityTests
{
    [Fact]
    public void Controller_ShouldBeRestrictedToAdmin()
    {
        var type = typeof(AdminInventoryManagementController);
        var attribute = type.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("Admin", attribute.Roles);
    }

    [Fact]
    public void Controller_ShouldBeInAdminArea()
    {
        var type = typeof(AdminInventoryManagementController);
        var attribute = type.GetCustomAttribute<AreaAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("Admin", attribute.RouteValue);
    }

    [Theory]
    [InlineData("Create", typeof(CreateProductViewModel))]
    [InlineData("Edit", typeof(CreateProductViewModel))]
    [InlineData("Delete", typeof(int))]
    public void PostMethods_ShouldHaveValidateAntiForgeryToken(string methodName, Type paramType)
    {
        var method = typeof(AdminInventoryManagementController).GetMethod(methodName, [paramType]);
        var attribute = method?.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();

        Assert.NotNull(attribute);
    }
}
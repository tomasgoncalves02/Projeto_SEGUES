using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Inventory.Controllers;
using System.Reflection;

namespace SeguesTests.SecurityTests.Inventory;

public class InventoryControllerSecurityTests
{
    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var controllerType = typeof(InventoryController);
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void Controller_HasAreaAttribute_WithCorrectName()
    {
        var controllerType = typeof(InventoryController);
        var areaAttribute = controllerType.GetCustomAttribute<AreaAttribute>();

        Assert.NotNull(areaAttribute);
        Assert.Equal("Inventory", areaAttribute.RouteValue);
    }
}
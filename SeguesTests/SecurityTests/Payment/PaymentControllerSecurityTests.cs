using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Projeto_SEGUES.Areas.Payment.Controllers;

namespace SeguesTests.SecurityTests.Payment;

public class PaymentControllerSecurityTests
{
    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var controllerType = typeof(PaymentController);
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void Controller_HasAreaAttribute_WithCorrectName()
    {
        var controllerType = typeof(PaymentController);
        var areaAttribute = controllerType.GetCustomAttribute<AreaAttribute>();

        Assert.NotNull(areaAttribute);
        Assert.Equal("Payment", areaAttribute.RouteValue);
    }

    [Fact]
    public void CreateCheckoutSession_HasValidateAntiForgeryToken()
    {
        var method = typeof(PaymentController).GetMethod("CreateCheckoutSession");
        var antiForgeryAttribute = method?.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();

        Assert.NotNull(antiForgeryAttribute);
    }

    [Fact]
    public void CreateCheckoutSession_IsPostMethod()
    {
        var method = typeof(PaymentController).GetMethod("CreateCheckoutSession");
        var postAttribute = method?.GetCustomAttribute<HttpPostAttribute>();

        Assert.NotNull(postAttribute);
    }
}
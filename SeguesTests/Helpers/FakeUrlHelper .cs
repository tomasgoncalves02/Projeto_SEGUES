using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace SeguesTests.Helpers;

public class FakeUrlHelper : IUrlHelper
{
    public ActionContext ActionContext { get; set; } = new();
    public string? Action(UrlActionContext actionContext) => null;
    public string? Content(string? contentPath) => contentPath;
    public bool IsLocalUrl(string? url) => true;
    public string RouteUrl(UrlRouteContext routeContext) => "/Identity/Account/Login";
    public string Link(string? routeName, object? values) => "/Identity/Account/Login";
}
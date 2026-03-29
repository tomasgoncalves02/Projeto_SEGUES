using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeguesTests.Helpers
{
    public class FakeUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; set; } = new ActionContext();
        public string? Action(UrlActionContext actionContext) => null;
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? RouteUrl(UrlRouteContext routeContext) => "/Identity/Account/Login";
        public string? Link(string? routeName, object? values) => "/Identity/Account/Login";

    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SeguesTests.Helpers;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Context.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader)) return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Pedro"),
            new(ClaimTypes.NameIdentifier, "pedro-77")
        };

        var parts = authHeader.Split(' ');
        if (parts.Length > 1)
        {
            string requestedRole = parts[1];
            claims.Add(new Claim(ClaimTypes.Role, requestedRole));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
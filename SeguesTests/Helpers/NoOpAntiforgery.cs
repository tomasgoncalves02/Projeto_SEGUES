using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace SeguesTests.Helpers;

public class NoOpAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext c) =>
        new("test", "test", "test", "test");

    public AntiforgeryTokenSet GetTokens(HttpContext c) =>
        new("test", "test", "test", "test");

    public Task<bool> IsRequestValidAsync(HttpContext c) =>
        Task.FromResult(true);

    public void SetCookieTokenAndHeader(HttpContext c) { }

    public Task ValidateRequestAsync(HttpContext c) =>
        Task.CompletedTask;
}
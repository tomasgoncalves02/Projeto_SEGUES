// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account;

/// <summary>
/// Model responsible for terminating the user's authenticated session.
/// </summary>
/// <remarks>
/// This class utilizes the <see cref="SignInManager{TUser}"/> to clear authentication 
/// cookies and ensure that the user's identity is removed from the application context.
/// </remarks>
[IgnoreAntiforgeryToken]
[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<LogoutModel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="LogoutModel"/>.
    /// </summary>
    /// <param name="signInManager">Authentication manager to process the session termination.</param>
    /// <param name="logger">Logging service to record user sign-outs.</param>
    public LogoutModel(SignInManager<AppUser> signInManager, ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Processes the logout request (POST) and redirects the user.
    /// </summary>
    /// <param name="returnUrl">Optional URL where the user should be sent after signing out.</param>
    /// <returns>
    /// A <see cref="LocalRedirect"/> if the URL is local, or the default page redirection.
    /// </returns>
    /// <remarks>
    /// The session termination is performed asynchronously to ensure that all 
    /// authentication resources are correctly released before responding to the browser.
    /// </remarks>
    public async Task<IActionResult> OnPost(string returnUrl = null)
    {
        // Identity sign out
        await _signInManager.SignOutAsync();

        _logger.LogInformation("Utilizador efetuou logout com sucesso.");

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home", new { area = "" });
    }
}
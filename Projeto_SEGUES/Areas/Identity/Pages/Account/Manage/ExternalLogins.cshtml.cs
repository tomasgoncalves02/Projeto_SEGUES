// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Page model for managing external logins (e.g., Google, Facebook) associated with the user account.
    /// </summary>
    /// <remarks>
    /// This class allows users to link new authentication providers or remove existing ones, 
    /// ensuring the user always maintains at least one way to access the account (password or another login).
    /// </remarks>
    public class ExternalLoginsModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IUserStore<AppUser> _userStore;

        /// <summary>
        /// Initializes a new instance of <see cref="ExternalLoginsModel"/>.
        /// </summary>
        /// <param name="userManager">User manager for manipulating login schemes.</param>
        /// <param name="signInManager">Authentication manager for configuring external properties.</param>
        /// <param name="userStore">User store for password hash verification.</param>
        public ExternalLoginsModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IUserStore<AppUser> userStore)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userStore = userStore;
        }

        /// <summary>
        /// List of login providers currently associated with the user's account.
        /// </summary>
        public IList<UserLoginInfo> CurrentLogins { get; set; } = new List<UserLoginInfo>();

        /// <summary>
        /// List of available external authentication schemes not yet linked to the account.
        /// </summary>
        public IList<AuthenticationScheme> OtherLogins { get; set; } = new List<AuthenticationScheme>();

        /// <summary>
        /// Determines if the removal button should be displayed, preventing the user from being left without access methods.
        /// </summary>
        public bool ShowRemoveButton { get; set; }

        /// <summary>
        /// Processes the page load, retrieving current logins and available schemes.
        /// </summary>
        /// <returns>The management page or NotFound if the user is invalid.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            CurrentLogins = await _userManager.GetLoginsAsync(user);
            OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();

            string? passwordHash = null;
            if (_userStore is IUserPasswordStore<AppUser> userPasswordStore)
            {
                passwordHash = await userPasswordStore.GetPasswordHashAsync(user, HttpContext.RequestAborted);
            }

            ShowRemoveButton = passwordHash != null || CurrentLogins.Count > 1;
            return Page();
        }

        /// <summary>
        /// Removes the association between the user account and a specific external login provider.
        /// </summary>
        /// <param name="loginProvider">The name of the provider (e.g., Google).</param>
        /// <param name="providerKey">The user's unique key at the external provider.</param>
        /// <returns>Redirect to the page with success or error feedback.</returns>
        public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                TempData.SetSwalError("Não foi possível remover o login externo.");
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData.SetSwalSuccess("O login externo foi removido.");
            return RedirectToPage();
        }

        /// <summary>
        /// Initiates the linking process for a new external provider by redirecting to a ChallengeResult.
        /// </summary>
        /// <param name="provider">The name of the provider to link.</param>
        /// <returns>A <see cref="ChallengeResult"/> that redirects to the external provider.</returns>
        public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        /// <summary>
        /// Callback processed after the user authorizes the linking at the external provider.
        /// </summary>
        /// <returns>Redirect to the main management page with the operation result.</returns>
        /// <exception cref="InvalidOperationException">Thrown if external login info cannot be retrieved.</exception>
        public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userId = await _userManager.GetUserIdAsync(user);
            var info = await _signInManager.GetExternalLoginInfoAsync(userId);
            if (info == null)
            {
                throw new InvalidOperationException("Não foi possível gerir os logins externos.");
            }

            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                TempData.SetSwalError("Não foi possível adicionar o login externo. Os logins externos só podem ser associados a uma conta.");
                return RedirectToPage();
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            TempData.SetSwalSuccess("O login externo foi adicionado.");
            return RedirectToPage();
        }
    }
}
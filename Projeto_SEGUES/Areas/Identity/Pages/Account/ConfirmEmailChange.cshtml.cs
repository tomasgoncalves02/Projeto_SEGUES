using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.Text;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account;

/// <summary>
/// Model responsible for confirming the change of the user's email address.
/// </summary>
/// <remarks>
/// This page is the destination for the confirmation link sent by email. It validates the security token,
/// updates the email address in the database, and synchronizes the username.
/// </remarks>
public class ConfirmEmailChangeModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<ConfirmEmailChangeModel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ConfirmEmailChangeModel"/>.
    /// </summary>
    /// <param name="userManager">User manager for credential updates.</param>
    /// <param name="signInManager">Sign-in manager to renew the session after the change.</param>
    /// <param name="logger">Logger service for tracking operations and errors.</param>
    public ConfirmEmailChangeModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ILogger<ConfirmEmailChangeModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Processes the email change confirmation through parameters received in the URL.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="email">The new email address to be confirmed.</param>
    /// <param name="code">The security code (token) encoded in Base64.</param>
    /// <returns>
    /// Redirects to the user profile with a success or error message (SweetAlert).
    /// </returns>
    public async Task<IActionResult> OnGetAsync(string? userId, string? email, string? code)
    {
        if (userId == null || email == null || code == null)
        {
            _logger.LogAppError(AppErrors.BadRequest, TableName.All, AppOperation.Other);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.BadRequest });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogAppError(AppErrors.UserNotFound, TableName.Identity, AppOperation.Read);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.UserNotFound });
        }

        string decodedCode;
        try
        {
            decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.InvalidToken, TableName.Identity, AppOperation.Update, ex);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.InvalidToken });
        }

        var result = await _userManager.ChangeEmailAsync(user, email, decodedCode);

        if (!result.Succeeded)
        {
            _logger.LogAppUser($"Failed to confirm new email ({email}) for user {user.Email}: Token used or expired.", UserAction.Update);
            TempData.SetSwalError("O link de confirmação expirou ou já foi utilizado.");
            return RedirectToAction("Index", "User", new { area = "User" });
        }

        // Sync the username with the new email
        var setUserNameResult = await _userManager.SetUserNameAsync(user, email);

        if (!setUserNameResult.Succeeded)
        {
            _logger.LogAppError(AppErrors.DatabaseUpdateError, TableName.Identity, AppOperation.Update);
            TempData.SetSwalWarning("O email foi alterado, mas houve um erro ao sincronizar o nome de utilizador. Contacte o suporte.");
        }
        else
        {
            _logger.LogAppUser($"User changed email successfully to: {email}.", UserAction.Update);
            TempData.SetSwalSuccess("Email e Login atualizados com sucesso.");
        }

        // Refresh the sign-in session
        await _signInManager.RefreshSignInAsync(user);

        return RedirectToAction("Index", "User", new { area = "User" });
    }
}
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
/// Model responsible for the initial confirmation of the user's email address.
/// </summary>
/// <remarks>
/// This page is the destination for the verification link sent after registering a new account, 
/// or when the user requests a resend of the confirmation link from their profile.
/// </remarks>
public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<ConfirmEmailModel> _logger;

    public ConfirmEmailModel(
        UserManager<AppUser> userManager,
        ILogger<ConfirmEmailModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Processes the email confirmation using parameters received in the URL.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="code">The security code (token) encoded in Base64.</param>
    /// <returns>Redirects to the Login page with visual feedback.</returns>
    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (userId == null || code == null)
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

        // Decode the code
        string decodedCode;
        try
        {
            decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (Exception ex)
        {
            // Register token manipulation error
            _logger.LogAppError(AppErrors.InvalidToken, TableName.Identity, AppOperation.Update, ex);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.InvalidToken });
        }

        // Confirm the email
        var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

        if (result.Succeeded)
        {
            _logger.LogAppUser($"Email of {user.Email} confirmed successfully.", UserAction.Update);
            TempData.SetSwalSuccess("A sua conta foi verificada com sucesso! Já pode iniciar sessão.");
        }
        else
        {
            // Token used or expired
            _logger.LogAppUser($"Fail to confirm email {user.Email}: Token used or expired.", UserAction.Update);
            TempData.SetSwalError("Erro ao confirmar o email. O link pode ter expirado ou já foi utilizado.");
        }

        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }
}
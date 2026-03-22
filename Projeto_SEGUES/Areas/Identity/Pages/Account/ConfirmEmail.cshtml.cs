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
/// Modelo responsável pela confirmação inicial do endereço de email do utilizador.
/// </summary>
/// <remarks>
/// Esta página é o destino do link de verificação enviado após o registo de uma nova conta,
/// ou quando o utilizador solicita o reenvio do link de confirmação no perfil.
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
    /// Processa a confirmação do email através dos parâmetros recebidos no URL.
    /// </summary>
    /// <param name="userId">O identificador único do utilizador.</param>
    /// <param name="code">O código (token) de segurança codificado em Base64.</param>
    /// <returns>Redireciona para a página de Login com feedback visual.</returns>
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
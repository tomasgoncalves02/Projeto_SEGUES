// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account;

/// <summary>
/// Model responsible for the password recovery request logic (forgot password).
/// </summary>
/// <remarks>
/// This class manages the delivery of a reset token via email, ensuring that only 
/// users with confirmed emails can initiate the recovery process.
/// </remarks>
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ForgotPasswordModel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ForgotPasswordModel"/>.
    /// </summary>
    /// <param name="userManager">User manager for account validation and token generation.</param>
    /// <param name="emailSender">Email sender service for user notification.</param>
    /// <param name="logger">Logger service for tracking errors and operations.</param>
    public ForgotPasswordModel(UserManager<AppUser> userManager, IEmailSender emailSender, ILogger<ForgotPasswordModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    /// <summary>
    /// Carries the user email submitted in the form.
    /// </summary>
    [BindProperty]
    public required InputModel Input { get; set; }

    /// <summary>
    /// Defines the validation rules for the password recovery request.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Email associated with the account to be recovered.
        /// </summary>
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Endereço de email inválido.")]
        [Display(Name = "Endereço de email")]
        public required string Email { get; init; }
    }

    /// <summary>
    /// Processes the submission of the password reset request.
    /// </summary>
    /// <returns>
    /// Always redirects to the confirmation page to prevent account enumeration (security).
    /// </returns>
    /// <remarks>
    /// The method generates a unique token via <see cref="UserManager{TUser}.GeneratePasswordResetTokenAsync"/>,
    /// encodes it in Base64, and sends a formatted email with the reset link.
    /// </remarks>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.FindByEmailAsync(Input.Email);

        // If user does not exist or email is not confirmed, redirect to ForgotPasswordConfirmation page.
        // This prevents hackers from trying to enumerate user accounts by brute force.
        if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        var email = await _userManager.GetEmailAsync(user);

        // Generate token
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(
            await _userManager.GeneratePasswordResetTokenAsync(user)
        ));

        // Callback link
        string callbackUrl = Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { area = "Identity", email, code },
            protocol: Request.Scheme)!;

        // Email template
        string emailBody = ((EmailSender)_emailSender).GetEmailBody(
            "Recuperação de Senha",
            user.FirstName,
            $"""
             <p>Recebemos um pedido para redefinir a password da tua conta na plataforma <strong>SEGUES</strong>.</p>
                                 <p>Para escolher uma nova password, clica no botão abaixo:</p>
                                 <div class='button-container'>
                                     <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='button'>REDEFINIR SENHA</a>
                                 </div>
                                 <p>Se o botão não funcionar, copia e cola o seguinte link no teu navegador:</p>
                                 <p class='text-color-ips' style='word-break: break-all; font-size: 12px;'>{callbackUrl}</p>
                                 <div class='security-note'>
                                     <p><strong>Nota de Segurança:</strong> Se não solicitaste esta alteração, podes ignorar este email com segurança. O link é válido por tempo limitado.</p>
                                 </div>
             """);

        // Send email
        try
        {
            await _emailSender.SendEmailAsync(
                Input.Email,
                "SEGUES - Recuperação de Senha",
                emailBody);
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.EmailSenderError, TableName.All, AppOperation.Other, ex);
        }

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}
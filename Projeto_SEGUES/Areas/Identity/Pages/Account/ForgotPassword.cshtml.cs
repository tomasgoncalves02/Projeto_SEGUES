// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<AppUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // Identity
        [BindProperty]
        public required InputModel Input { get; set; }
        
        public class InputModel
        {
            [Required]
            [EmailAddress]
            public required string Email { get; init; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                {
                    // Not revealed to the attacker that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }
                
                var email = await _userManager.GetEmailAsync(user);
                
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(
                        await _userManager.GeneratePasswordResetTokenAsync(user)
                ));
                string callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", email, code },
                    protocol: Request.Scheme)!;

                // Email body
                string emailBody = ((EmailSender) _emailSender).GetEmailBody(
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

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "SEGUES - Recuperação de Senha",
                    emailBody);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}

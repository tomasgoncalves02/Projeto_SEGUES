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

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<User> userManager, IEmailSender emailSender)
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
                
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(
                        await _userManager.GeneratePasswordResetTokenAsync(user)
                ));
                string callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme)!;

                // Email template
                // Note: In C# interpolated strings ($""), CSS braces must be doubled ({{ }})
                string emailBody = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7f6; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
                .header {{ background-color: #009697; padding: 30px; text-align: center; color: #ffffff; }}
                .header h1 {{ margin:0; font-size: 28px; color: #ffffff !important; }}
                .content {{ padding: 40px; line-height: 1.6; color: #333333; }}
                .button-container {{ text-align: center; margin: 30px 0; }}
                .button {{ background-color: #009697; color: #ffffff !important; padding: 15px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block; }}
                .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #777777; }}
                .security-note {{ border-top: 1px solid #eeeeee; margin-top: 30px; padding-top: 20px; font-size: 13px; color: #999999; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1 style='color: white;'>SEGUES</h1>
                    <p style='margin:0; opacity: 0.8; color: white;'>Controlo de Refeições</p>
                </div>
                <div class='content'>
                    <h2 style='color: #009697;'>Recuperação de Senha</h2>
                    <p>Olá,</p>
                    <p>Recebemos um pedido para redefinir a password da tua conta na plataforma <strong>SEGUES</strong>.</p>
                    <p>Para escolher uma nova password, clica no botão abaixo:</p>
                    <div class='button-container'>
                        <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='button'>REDEFINIR SENHA</a>
                    </div>
                    <p>Se o botão não funcionar, copia e cola o seguinte link no teu navegador:</p>
                    <p style='word-break: break-all; color: #009697; font-size: 12px;'>{callbackUrl}</p>
                    <div class='security-note'>
                        <p><strong>Nota de Segurança:</strong> Se não solicitaste esta alteração, podes ignorar este email com segurança. O link é válido por tempo limitado.</p>
                    </div>
                </div>
                <div class='footer'>
                    &copy; 2026 SEGUES - Sistema de Gestão de Refeições.
                </div>
            </div>
        </body>
        </html>";

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

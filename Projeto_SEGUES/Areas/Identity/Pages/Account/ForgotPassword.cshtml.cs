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
    /// <summary>
    /// Model responsável pela lógica de solicitação de recuperação de senha (esqueci-me da senha).
    /// </summary>
    /// <remarks>
    /// Esta classe gere o envio de um token de redefinição via email, garantindo que apenas 
    /// utilizadores com email confirmado possam iniciar o processo de recuperação.
    /// </remarks>
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="ForgotPasswordModel"/>.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores para validação de conta e geração de tokens.</param>
        /// <param name="emailSender">Serviço de envio de emails para notificação do utilizador.</param>
        public ForgotPasswordModel(UserManager<AppUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Transporta o email do utilizador submetido no formulário.
        /// </summary>
        [BindProperty]
        public required InputModel Input { get; set; }

        /// <summary>
        /// Define as regras de validação para o pedido de recuperação de senha.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Email associado à conta que pretende recuperar.
            /// </summary>
            [Required]
            [EmailAddress]
            public required string Email { get; init; }
        }

        /// <summary>
        /// Processa o envio do pedido de redefinição de senha.
        /// </summary>
        /// <returns>
        /// Redireciona sempre para a página de confirmação para evitar a enumeração de contas (segurança).
        /// </returns>
        /// <remarks>
        /// O método gera um token único via <see cref="UserManager{TUser}.GeneratePasswordResetTokenAsync"/>,
        /// codifica-o em Base64 e envia um email formatado com o link de redefinição.
        /// </remarks>
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);

                // Validação de segurança: se o user não existir ou o email não estiver confirmado, 
                // redirecionamos na mesma para não dar pistas a atacantes.
                if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                {
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                var email = await _userManager.GetEmailAsync(user);

                // Geração e codificação do token de segurança
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(
                        await _userManager.GeneratePasswordResetTokenAsync(user)
                ));

                // Construção do link de callback para a página de ResetPassword
                string callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", email, code },
                    protocol: Request.Scheme)!;

                // Construção do corpo do email com HTML personalizado
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

                // Envio do email através do serviço configurado
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
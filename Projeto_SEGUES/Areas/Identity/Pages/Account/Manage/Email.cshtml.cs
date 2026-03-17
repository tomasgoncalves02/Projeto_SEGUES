// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Classe de modelo para a página de gestão de email do utilizador.
    /// </summary>
    /// <remarks>
    /// Esta classe permite ao utilizador visualizar o seu email atual, verificar o estado de confirmação 
    /// e solicitar a alteração do endereço através do envio de tokens de segurança por email.
    /// </remarks>
    public class EmailModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="EmailModel"/>.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores para operações de conta.</param>
        /// <param name="emailSender">Serviço de envio de emails para notificações e tokens.</param>
        public EmailModel(
            UserManager<AppUser> userManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Obtém ou define o email atual do utilizador.
        /// </summary>
        public required string Email { get; set; }

        /// <summary>
        /// Indica se o email atual já foi confirmado pelo utilizador.
        /// </summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>
        /// Modelo de entrada de dados para o formulário de alteração de email.
        /// </summary>
        [BindProperty]
        public required InputModel Input { get; set; }

        /// <summary>
        /// Define as propriedades e validações do formulário de entrada para novo email.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// O novo endereço de email pretendido pelo utilizador.
            /// </summary>
            [Required(ErrorMessage = "O email é obrigatório.")]
            [EmailAddress(ErrorMessage = "Email inválido.")]
            [Display(Name = "Novo email")]
            public required string NewEmail { get; init; }
        }

        /// <summary>
        /// Carrega os dados do utilizador para as propriedades do modelo.
        /// </summary>
        /// <param name="user">O utilizador autenticado atual.</param>
        private async Task LoadAsync(AppUser user)
        {
            var email = (await _userManager.GetEmailAsync(user))!;
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        /// <summary>
        /// Processa o pedido GET inicial para a página de gestão de email.
        /// </summary>
        /// <returns>A página Razor com os dados carregados.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Não foi possível alterar o email.");
            }

            await LoadAsync(user);
            return Page();
        }

        /// <summary>
        /// Processa o pedido de alteração de email e envia o link de confirmação para o novo endereço.
        /// </summary>
        /// <returns>Redirecionamento para a mesma página com mensagem de sucesso ou erro.</returns>
        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Não foi possível alterar o email.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                email = Input.NewEmail;
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId, email, code },
                    protocol: Request.Scheme)!;
                await _emailSender.SendEmailAsync(
                    email,
                    "Confirma o teu email",
                    $"Por favor confirma a tua conta <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>.");
                TempData.SetSwalSuccess("Link de confirmação para alterar o email enviado. Por favor verifica o teu email.");
                return RedirectToPage();
            }
            TempData.SetSwalInfo("O teu email permanece inalterado.");
            return RedirectToPage();
        }

        /// <summary>
        /// Reenvia o email de confirmação para o endereço de email atual, caso este ainda não esteja confirmado.
        /// </summary>
        /// <returns>Redirecionamento para a página atual com feedback visual.</returns>
        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Não foi possível enviar o email de verificação.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var email = (await _userManager.GetEmailAsync(user))!;
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailChange",
                pageHandler: null,
                values: new { area = "Identity", userId, email, code },
                protocol: Request.Scheme)!;
            await _emailSender.SendEmailAsync(
                email,
                "Confirma o teu email",
                $"Por favor confirma a tua conta <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>..");

            TempData.SetSwalSuccess("Link de confirmação para alterar o email enviado. Por favor verifica o teu email.");
            return RedirectToPage();
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Modelo responsável pela confirmação da alteração do endereço de email do utilizador.
    /// </summary>
    /// <remarks>
    /// Esta página é o destino do link de confirmação enviado por email. Ela valida o token de segurança,
    /// atualiza o endereço de email na base de dados e sincroniza o nome de utilizador (Username).
    /// </remarks>
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="ConfirmEmailChangeModel"/>.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores para atualização de credenciais.</param>
        /// <param name="signInManager">Gestor de autenticação para renovar a sessão após a alteração.</param>
        public ConfirmEmailChangeModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Processa a confirmação da alteração de email através dos parâmetros recebidos no URL.
        /// </summary>
        /// <param name="userId">O identificador único do utilizador.</param>
        /// <param name="email">O novo endereço de email a ser confirmado.</param>
        /// <param name="code">O código (token) de segurança codificado em Base64.</param>
        /// <returns>
        /// Redireciona para o perfil do utilizador com uma mensagem de sucesso ou erro (SweetAlert).
        /// </returns>
        public async Task<IActionResult> OnGetAsync(string? userId, string? email, string? code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Não foi possível alterar o email. Tente novamente mais tarde ou contacte o suporte.");
            }

            // Descodifica o token de segurança recebido no URL
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var currentEmail = (await _userManager.GetEmailAsync(user))!;

            // Tenta efetuar a alteração do email no Identity
            var result = await _userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                TempData.SetSwalError("Erro ao alterar email. Tente novamente mais tarde ou contacte o suporte.");
                return RedirectToAction("Index", "User", new { area = "User" });
            }

            // Atualiza o UserName para manter a consistência (Username = Email)
            var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
            if (!setUserNameResult.Succeeded)
            {
                // Rollback: se falhar a atualização do Username, reverte o email para o anterior
                await _userManager.ChangeEmailAsync(user, currentEmail, code);

                TempData.SetSwalError("Erro ao alterar email. Tente novamente mais tarde ou contacte o suporte.");
                return RedirectToAction("Index", "User", new { area = "User" });
            }

            // Atualiza o cookie de autenticação para refletir as novas credenciais
            await _signInManager.RefreshSignInAsync(user);
            TempData.SetSwalSuccess("Email alterado com sucesso.");
            return RedirectToAction("Index", "User", new { area = "User" });
        }
    }
}
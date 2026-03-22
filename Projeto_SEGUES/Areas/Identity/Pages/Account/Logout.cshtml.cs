// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model responsável por encerrar a sessão autenticada do utilizador.
    /// </summary>
    /// <remarks>
    /// Esta classe utiliza o <see cref="SignInManager{TUser}"/> para limpar os cookies 
    /// de autenticação e garantir que a identidade do utilizador é removida do contexto da aplicação.
    /// </remarks>
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="LogoutModel"/>.
        /// </summary>
        /// <param name="signInManager">Gestor de autenticação para processar o encerramento de sessão.</param>
        /// <param name="logger">Serviço de logging para registar a saída de utilizadores.</param>
        public LogoutModel(SignInManager<AppUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Processa o pedido de logout (POST) e redireciona o utilizador.
        /// </summary>
        /// <param name="returnUrl">URL opcional para onde o utilizador deve ser enviado após sair.</param>
        /// <returns>
        /// Um <see cref="LocalRedirect"/> se o URL for local, ou o redirecionamento padrão da página.
        /// </returns>
        /// <remarks>
        /// O encerramento de sessão é feito de forma assíncrona para garantir que todos os 
        /// recursos de autenticação são libertados corretamente antes da resposta ao browser.
        /// </remarks>
        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // Identity sign out
            await _signInManager.SignOutAsync();

            _logger.LogInformation("Utilizador efetuou logout com sucesso.");

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
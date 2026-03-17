// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Model da página de gestão de logins externos (ex: Google, Facebook) associados à conta do utilizador.
    /// </summary>
    /// <remarks>
    /// Esta classe permite ao utilizador vincular novos fornecedores de autenticação ou remover os existentes,
    /// garantindo sempre que o utilizador mantém pelo menos uma forma de aceder à conta (password ou outro login).
    /// </remarks>
    public class ExternalLoginsModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IUserStore<AppUser> _userStore;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="ExternalLoginsModel"/>.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores para manipulação de esquemas de login.</param>
        /// <param name="signInManager">Gestor de autenticação para configurar propriedades externas.</param>
        /// <param name="userStore">Armazenamento de utilizadores para verificação de hash de password.</param>
        public ExternalLoginsModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IUserStore<AppUser> userStore)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userStore = userStore;
        }

        /// <summary>
        /// Lista dos fornecedores de login atualmente associados à conta do utilizador.
        /// </summary>
        public IList<UserLoginInfo> CurrentLogins { get; set; } = new List<UserLoginInfo>();

        /// <summary>
        /// Lista de esquemas de autenticação externa disponíveis que ainda não estão vinculados à conta.
        /// </summary>
        public IList<AuthenticationScheme> OtherLogins { get; set; } = new List<AuthenticationScheme>();

        /// <summary>
        /// Define se o botão de remoção deve ser exibido, prevenindo que o utilizador fique sem métodos de acesso.
        /// </summary>
        public bool ShowRemoveButton { get; set; }

        /// <summary>
        /// Processa o carregamento da página, obtendo os logins atuais e os esquemas disponíveis.
        /// </summary>
        /// <returns>A página de gestão ou NotFound caso o utilizador seja inválido.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Não foi possível gerir os logins externos.");
            }

            CurrentLogins = await _userManager.GetLoginsAsync(user);
            OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();

            string? passwordHash = null;
            if (_userStore is IUserPasswordStore<AppUser> userPasswordStore)
            {
                passwordHash = await userPasswordStore.GetPasswordHashAsync(user, HttpContext.RequestAborted);
            }

            ShowRemoveButton = passwordHash != null || CurrentLogins.Count > 1;
            return Page();
        }

        /// <summary>
        /// Remove a associação entre a conta do utilizador e um fornecedor de login externo específico.
        /// </summary>
        /// <param name="loginProvider">O nome do fornecedor (ex: Google).</param>
        /// <param name="providerKey">A chave única do utilizador no fornecedor externo.</param>
        /// <returns>Redirecionamento para a página com feedback de sucesso ou erro.</returns>
        public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Não foi possível gerir os logins externos.");
            }

            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                TempData.SetSwalError("Não foi possível remover o login externo.");
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData.SetSwalSuccess("O login externo foi removido.");
            return RedirectToPage();
        }

        /// <summary>
        /// Inicia o processo de vinculação de um novo fornecedor externo, redirecionando para o ChallengeResult.
        /// </summary>
        /// <param name="provider">O nome do fornecedor a vincular.</param>
        /// <returns>Um <see cref="ChallengeResult"/> que redireciona para o fornecedor externo.</returns>
        public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        /// <summary>
        /// Callback processado após o utilizador autorizar a vinculação no fornecedor externo.
        /// </summary>
        /// <returns>Redirecionamento para a página principal de gestão com o resultado da operação.</returns>
        /// <exception cref="InvalidOperationException">Lançada se os dados do login externo não puderem ser recuperados.</exception>
        public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Não foi possível gerir os logins externos..");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var info = await _signInManager.GetExternalLoginInfoAsync(userId);
            if (info == null)
            {
                throw new InvalidOperationException("Não foi possível gerir os logins externos.");
            }

            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                TempData.SetSwalError("Não foi possível adicionar o login externo. Os logins externos só podem ser associados a uma conta.");
                return RedirectToPage();
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            TempData.SetSwalSuccess("O login externo foi adicionado.");
            return RedirectToPage();
        }
    }
}
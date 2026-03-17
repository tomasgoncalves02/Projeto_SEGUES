// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// Classe de modelo para a página de alteração de palavra-passe do utilizador.
    /// </summary>
    /// <remarks>
    /// Esta classe gere a lógica de validação das credenciais atuais e a atualização para uma nova 
    /// palavra-passe no sistema de identidade, garantindo o cumprimento dos requisitos de segurança.
    /// </remarks>
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ChangePasswordModel"/>.
        /// </summary>
        /// <param name="userManager">Serviço para gestão de utilizadores Identity.</param>
        /// <param name="signInManager">Serviço para gestão de autenticação e sessões.</param>
        /// <param name="logger">Serviço para registo de eventos e erros.</param>
        public ChangePasswordModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Obtém ou define o modelo de entrada de dados do formulário.
        /// </summary>
        [BindProperty]
        public required InputModel Input { get; set; }

        /// <summary>
        /// Define a estrutura de dados e as regras de validação para a alteração de palavra-passe.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Palavra-passe atual do utilizador.
            /// </summary>
            [Required(ErrorMessage = "O campo {0} é obrigatório.")]
            [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "Password Actual")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
                ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo. E no mínimo 12 caracteres.")]
            public required string OldPassword { get; init; }

            /// <summary>
            /// Nova palavra-passe pretendida pelo utilizador.
            /// </summary>
            [Required(ErrorMessage = "O campo {0} é obrigatório.")]
            [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "Nova Password")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
                ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo. E no mínimo 12 caracteres.")]
            public required string NewPassword { get; init; }

            /// <summary>
            /// Confirmação da nova palavra-passe.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar password")]
            [Compare("Password", ErrorMessage = "A password e a confirmação não coincidem.")]
            public required string ConfirmPassword { get; init; }
        }

        /// <summary>
        /// Processa o pedido GET inicial para a página de alteração de palavra-passe.
        /// </summary>
        /// <returns>A página Razor correspondente ou um erro caso o utilizador não seja encontrado.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Não foi possível alterar a password.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToPage("/Account/ResetPassword", new { Area = "Identity" });
            }

            return Page();
        }

        /// <summary>
        /// Processa a submissão do formulário de alteração de palavra-passe.
        /// </summary>
        /// <returns>
        /// Redirecionamento para o perfil do utilizador em caso de sucesso ou a página atual com mensagens de erro em caso de falha.
        /// </returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Não foi possível alterar a password.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Utilizador alterou a password com sucesso.");
            TempData.SetSwalSuccess("A sua password foi alterada com sucesso.");
            return RedirectToAction("Index", "User", new { area = "User" });
        }
    }
}
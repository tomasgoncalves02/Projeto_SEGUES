// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model responsável pela lógica de autenticação de utilizadores no sistema SEGUES.
    /// </summary>
    /// <remarks>
    /// Gere o processo de login local, autenticação externa (OAuth), verificação de estado da conta (Ativo/Inativo)
    /// e mecanismos de segurança como bloqueio por tentativas falhadas (Lockout) e 2FA.
    /// </remarks>
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<AppUser> _userManager;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="LoginModel"/>.
        /// </summary>
        public LoginModel(SignInManager<AppUser> signInManager, ILogger<LoginModel> logger, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Modelo de entrada de dados para as credenciais de login.
        /// </summary>
        [BindProperty]
        public required InputModel Input { get; set; }

        /// <summary>
        /// Lista de fornecedores de autenticação externa configurados (ex: Google).
        /// </summary>
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        /// <summary>
        /// URL de redirecionamento após o sucesso da autenticação.
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Armazena mensagens de erro temporárias vindas de redirecionamentos.
        /// </summary>
        [TempData]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Define a estrutura e validações do formulário de login.
        /// </summary>
        public class InputModel
        {
            /// <summary>Identificador único de email do utilizador.</summary>
            [Required(ErrorMessage = "O email é obrigatório.")]
            [EmailAddress(ErrorMessage = "Endereço de email inválido.")]
            [Display(Name = "Endereço de email")]
            public required string Email { get; init; }
        
            /// <summary>Palavra-passe de acesso.</summary>
            [Required(ErrorMessage = "A password é obrigatória.")]
            [StringLength(100, ErrorMessage = "A password deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
                ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo. E no mínimo 12 caracteres.")]
            [Display(Name = "Password")]
            public required string Password { get; init; }
        
            /// <summary>Define se o cookie de autenticação deve persistir após fechar o navegador.</summary>
            [Display(Name = "Lembrar-me")]
            public bool RememberMe { get; init; }
        }

        /// <summary>
        /// Prepara a página de login para apresentação (GET).
        /// </summary>
        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError("", ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");
            
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        /// <summary>
        /// Processa a tentativa de autenticação do utilizador (POST).
        /// </summary>
        /// <returns>Redirecionamento em caso de sucesso ou a página com erros em caso de falha.</returns>
        /// <remarks>
        /// O fluxo inclui:
        /// 1. Verificação da existência do utilizador.
        /// 2. Validação do estado da conta (bloqueio administrativo se <see cref="UserStatus.Inactive"/>).
        /// 3. Verificação de credenciais via <see cref="SignInManager{TUser}.PasswordSignInAsync(string, string, bool, bool)"/>.
        /// </remarks>
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kv => kv.Value is not null && kv.Value.Errors.Count > 0)
                    .SelectMany(kv => kv.Value!.Errors.Select(err => new { Field = kv.Key, Error = err.ErrorMessage }))
                    .ToList();
        
                foreach (var fe in fieldErrors)
                {
                    _logger.LogWarning("Validation error on field {Field}: {Error}", fe.Field, fe.Error);
                }
        
                TempData.SetSwalError("Por favor corrija os erros no formulário.");
                return Page();
            }
            
            // Not allow login if the user is inactive
            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user is { Status: UserStatus.Inactive })
            {
                _logger.LogWarning("Tentativa de login em conta desativada: {Email}", Input.Email);
                TempData.SetSwalError("A sua conta foi desativada pela administração.");
                return Page();
            }

            // Identity Authentication
            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Utilizador {Email} autenticado com sucesso.", Input.Email);
                _logger.LogAppUser($"User {Input.Email} logged in successfully.", UserAction.LogIn);
                TempData.SetSwalSuccess("Login efetuado com sucesso!");
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = "/", Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Conta {Email} bloqueada por excesso de tentativas falhadas.", Input.Email);
                return RedirectToPage("./Lockout");
            }
            
            _logger.LogWarning("Falha no login para {Email}: Credenciais inválidas.", Input.Email);
            TempData.SetSwalError("Tentativa de login inválida.");
            return Page();
        }
    }
}
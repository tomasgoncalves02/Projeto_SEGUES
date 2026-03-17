using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model responsável pela segunda etapa da autenticação (2FA - Two-Factor Authentication).
    /// </summary>
    /// <remarks>
    /// Esta página é invocada quando o <see cref="LoginModel"/> deteta que o utilizador tem o 2FA ativo.
    /// Utiliza tokens baseados em tempo (Authenticator Apps) para validar a identidade final do utilizador.
    /// </remarks>
    public class LoginWith2FaModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<LoginWith2FaModel> _logger;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="LoginWith2FaModel"/>.
        /// </summary>
        /// <param name="signInManager">Gestor de autenticação para validar o token 2FA.</param>
        /// <param name="logger">Serviço de logging para registar tentativas de login e bloqueios.</param>
        public LoginWith2FaModel(SignInManager<AppUser> signInManager, ILogger<LoginWith2FaModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Modelo de entrada para o código de autenticação de dois fatores.
        /// </summary>
        [BindProperty]
        public required InputModel Input { get; set; }

        /// <summary>
        /// Mantém o estado da opção "Lembrar-me" vinda da página de login inicial.
        /// </summary>
        public bool RememberMe { get; set; }

        /// <summary>
        /// URL para redirecionamento pós-autenticação.
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Estrutura de validação para o token 2FA.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Código numérico de 6 dígitos gerado pela aplicação autenticadora.
            /// </summary>
            [Required(ErrorMessage = "O código é obrigatório.")]
            [StringLength(7, ErrorMessage = "O {0} deve ter entre {2} e {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Código de autenticação")]
            public required string TwoFactorCode { get; init; }

            /// <summary>
            /// Se verdadeiro, o navegador será "lembrado", não solicitando 2FA neste dispositivo por um período definido.
            /// </summary>
            [Display(Name = "Lembrar este dispositivo")]
            public bool RememberMachine { get; init; }
        }

        /// <summary>
        /// Prepara a página de 2FA, garantindo que existe um utilizador no fluxo de autenticação.
        /// </summary>
        /// <param name="rememberMe">Estado da persistência da sessão.</param>
        /// <param name="returnUrl">Destino após login.</param>
        /// <returns>A página de introdução do código ou redirecionamento para Login se o contexto for perdido.</returns>
        public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
        {
            // Garante que o utilizador passou pela autenticação de password primeiro
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogError("Não foi possível carregar o utilizador de autenticação de dois fatores.");
                ModelState.AddModelError(string.Empty, "Não foi possível carregar o utilizador de autenticação de dois fatores.");
                return RedirectToPage("./Login");
            }

            ReturnUrl = returnUrl;
            RememberMe = rememberMe;
            return Page();
        }

        /// <summary>
        /// Processa e valida o código 2FA submetido pelo utilizador.
        /// </summary>
        /// <param name="rememberMe">Persistência da sessão.</param>
        /// <param name="returnUrl">Destino após sucesso.</param>
        /// <returns>Redirecionamento para o destino local ou página de bloqueio em caso de falhas excessivas.</returns>
        /// <remarks>
        /// O código é limpo de espaços ou hifens antes da validação via <see cref="SignInManager{TUser}.TwoFactorAuthenticatorSignInAsync"/>.
        /// </remarks>
        public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            returnUrl ??= Url.Content("~/");

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogError("Não foi possível carregar o utilizador de autenticação de dois fatores.");
                ModelState.AddModelError(string.Empty, "Não foi possível carregar o utilizador de autenticação de dois fatores.");
                return RedirectToPage("./Login");
            }

            // Normalização do código (remove caracteres de formatação comuns)
            var authenticatorCode = Input.TwoFactorCode.Replace(" ", "").Replace("-", "");

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("Utilizador com ID '{UserId}' fez login com 2FA.", user.Id);
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Utilizador com ID '{UserId}' bloqueado.", user.Id);
                return RedirectToPage("./Lockout");
            }

            _logger.LogWarning("Código de autenticação inválido para o utilizador ID '{UserId}'.", user.Id);
            ModelState.AddModelError(string.Empty, "Código de autenticação inválido.");
            return Page();
        }
    }
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class LoginWith2FaModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LoginWith2FaModel> _logger;

        public LoginWith2FaModel(SignInManager<User> signInManager, ILogger<LoginWith2FaModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public required InputModel Input { get; set; }

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "O {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Código de autenticação")]
            public required string TwoFactorCode { get; init; }

            [Display(Name = "Lembrar este dispositivo")]
            public bool RememberMachine { get; init; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
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
            _logger.LogWarning("Código de autenticação inválido introduzido para o utilizador com ID '{UserId}'.", user.Id);
            ModelState.AddModelError(string.Empty, "Código de autenticação inválido.");
            return Page();
        }
    }
}
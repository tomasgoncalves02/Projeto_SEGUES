using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model responsável pela redefinição da password de um utilizador através de um token de segurança.
    /// </summary>
    /// <remarks>
    /// Esta página valida o token enviado por email e permite ao utilizador definir uma nova credencial,
    /// aplicando as mesmas regras de complexidade do registo inicial.
    /// </remarks>
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="ResetPasswordModel"/>.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores para validar tokens e atualizar passwords.</param>
        public ResetPasswordModel(UserManager<AppUser> userManager, ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Modelo de entrada que contém os dados para a redefinição da password.
        /// </summary>
        [BindProperty]
        public required InputModel Input { get; set; }

        /// <summary>
        /// Estrutura de validação para os campos de redefinição de password.
        /// </summary>
        public class InputModel
        {
            /// <summary>Endereço de email do utilizador.</summary>
            [Required]
            public required string Email { get; init; }

            /// <summary>Nova password com validação de complexidade forte.</summary>
            [Required(ErrorMessage = "A password é obrigatória.")]
            [StringLength(100, ErrorMessage = "A password deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
                ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo. E no mínimo 12 caracteres.")]
            public required string Password { get; init; }

            /// <summary>Confirmação da nova password.</summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar password")]
            [Compare("Password", ErrorMessage = "A password e a confirmação não coincidem.")]
            public required string ConfirmPassword { get; init; }

            /// <summary>Código de segurança (token) gerado pelo sistema.</summary>
            [Required]
            public required string Code { get; init; }
        }

        /// <summary>
        /// Prepara a página de redefinição, descodificando o código recebido via URL.
        /// </summary>
        /// <param name="email">Email passado via QueryString.</param>
        /// <param name="code">Token de segurança codificado em Base64.</param>
        /// <returns>A página de formulário ou um erro se o token estiver ausente.</returns>
        public IActionResult OnGet(string? email = null, string? code = null)
        {
            if (email == null || code == null)
            {
                _logger.LogAppError(AppErrors.InvalidToken, TableName.All, AppOperation.Other);
                TempData.SetSwalError(AppErrors.InvalidToken.GetViewErrorMessage());
                return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.InvalidToken });
            }
            Input = new InputModel
            {
                Email = email,
                Password = "",
                ConfirmPassword = "",
                Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
            };
            return Page();
        }

        /// <summary>
        /// Processa a submissão da nova password.
        /// </summary>
        /// <returns>Redirecionamento para a página de sucesso ou recarregamento com erros.</returns>
        /// <remarks>
        /// Caso o utilizador possua uma conta criada por um administrador (ainda não confirmada),
        /// o sucesso nesta operação ativa automaticamente a propriedade <see cref="AppUser.EmailConfirmed"/>.
        /// </remarks>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // For security, doest not reveal that the user does not exist
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // Try to reset the password
            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                if (user.EmailConfirmed) return RedirectToPage("./ResetPasswordConfirmation");
                
                // Confirm email for users created by admins
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
                return RedirectToPage("./ResetPasswordConfirmation");
            }
            
            _logger.LogAppError(AppErrors.InvalidToken, TableName.All, AppOperation.Other);
            TempData.SetSwalError(AppErrors.InvalidToken.GetViewErrorMessage());
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.InvalidToken });
        }
    }
}
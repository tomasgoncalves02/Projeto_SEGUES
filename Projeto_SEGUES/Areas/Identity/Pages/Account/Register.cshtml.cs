using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Areas.Identity.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model responsável pelo processo de registo de novos utilizadores no sistema SEGUES.
    /// </summary>
    /// <remarks>
    /// Implementa um fluxo de verificação prévia, onde os dados do utilizador são validados 
    /// e um código de verificação é enviado por email antes da criação definitiva da conta na base de dados.
    /// </remarks>
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<RegisterModel> _logger;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="RegisterModel"/>.
        /// </summary>
        public RegisterModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IEmailSender emailSender,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        /// <summary>
        /// Modelo de dados que contém as informações de registo (Nome, Email, Password, etc.).
        /// </summary>
        [BindProperty]
        public required RegisterDataViewModel Input { get; set; }

        /// <summary>
        /// URL de destino após a conclusão do registo.
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Lista de fornecedores de autenticação externa disponíveis (ex: Google).
        /// </summary>
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        /// <summary>
        /// Prepara a página de registo e carrega os esquemas de autenticação externa.
        /// </summary>
        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        /// <summary>
        /// Processa a submissão do formulário de registo.
        /// </summary>
        /// <param name="returnUrl">URL de redirecionamento opcional.</param>
        /// <returns>Redirecionamento para a página de verificação de código ou a própria página com erros.</returns>
        /// <remarks>
        /// O fluxo consiste em:
        /// 1. Validar se o email já existe.
        /// 2. Gerar um código numérico de 6 dígitos.
        /// 3. Armazenar temporariamente os dados em <see cref="TempData"/> via JSON.
        /// 4. Enviar o email de boas-vindas com o código de validação.
        /// </remarks>
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid) return Page();

            // Check if email is already registered to prevent duplicates
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "Este email já está registado.");
                return Page();
            }

            // Generate code
            var verificationCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var registrationData = new RegisterDataViewModel
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Gender = Input.Gender,
                Email = Input.Email,
                BirthDate = Input.BirthDate,
                Password = Input.Password,
                ConfirmPassword = Input.ConfirmPassword,
                Code = verificationCode,
                ExpiryTime = DateTime.Now.AddMinutes(5)
            };
            
            // Serializate the data to be stored between requests (multi-step form)
            TempData.SetJson("RegistrationData", registrationData);

            var emailBody = ((EmailSender)_emailSender).GetEmailBody(
                "Bem-vindo ao SEGUES!",
                Input.FirstName,
                $"""
                 <div style='text-align: center;'>
                    <p>Use o código abaixo para criar a sua conta (expira em 5 minutos):</p>
                    <h1 style='background-color: #eee; padding: 10px; display: inline-block; letter-spacing: 5px;'>{verificationCode}</h1>
                 </div>
                 """);

            try
            {
                await _emailSender.SendEmailAsync(Input.Email, "Código de Validação SEGUES", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogAppError(AppErrors.SendActivationEmailError, TableName.All, AppOperation.Other, ex);
                TempData.Remove("RegistrationData");
                ModelState.AddModelError(string.Empty, "Falha ao enviar o email. Verifique a sua conexão ou tente mais tarde.");
                return Page();
            }

            return RedirectToPage("VerifyCode", new { returnUrl });
        }
    }
}
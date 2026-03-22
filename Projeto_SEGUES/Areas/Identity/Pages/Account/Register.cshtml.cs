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
    /// Model responsible for the registration process of new users in the SEGUES system.
    /// </summary>
    /// <remarks>
    /// Implements a pre-verification flow where user data is validated 
    /// and a verification code is sent via email before the final account creation in the database.
    /// </remarks>
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<RegisterModel> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="RegisterModel"/>.
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
        /// Data model containing registration information (Name, Email, Password, etc.).
        /// </summary>
        [BindProperty]
        public required RegisterDataViewModel Input { get; set; }

        /// <summary>
        /// Target URL after registration completion.
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// List of available external authentication providers (e.g., Google).
        /// </summary>
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        /// <summary>
        /// Prepares the registration page and loads external authentication schemes.
        /// </summary>
        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        /// <summary>
        /// Processes the registration form submission.
        /// </summary>
        /// <param name="returnUrl">Optional redirection URL.</param>
        /// <returns>Redirection to the code verification page or the page itself with errors.</returns>
        /// <remarks>
        /// The flow consists of:
        /// 1. Validating if the email already exists.
        /// 2. Generating a 6-digit numeric code.
        /// 3. Temporarily storing data in <see cref="TempData"/> via JSON.
        /// 4. Sending the welcome email with the validation code.
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
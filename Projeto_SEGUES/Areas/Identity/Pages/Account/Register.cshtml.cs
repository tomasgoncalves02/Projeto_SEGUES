using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Areas.Identity.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<RegisterModel> _logger;

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

        // Identity
        [BindProperty]
        public required RegisterDataViewModel Input { get; set; }
        public string? ReturnUrl { get; set; }
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (!ModelState.IsValid) return Page();
            // Verify if email already exists
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "Este email já está registado.");
                return Page();
            }
                
            // Generate verification code and pass data
            var verificationCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var registrationData = new RegisterDataViewModel 
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Gender = Input.Gender,
                Email = Input.Email,
                Password = Input.Password,
                ConfirmPassword = Input.ConfirmPassword,
                Code = verificationCode,
                ExpiryTime = DateTime.Now.AddMinutes(5)
            };
            TempData.SetJson("RegistrationData", registrationData);
            try
            {
                await _emailSender.SendEmailAsync(Input.Email, "Código de Validação SEGUES",
                    $"<div style='font-family: Arial, sans-serif; text-align: center;'>" +
                    $"<h2 style='color: #2c3e50;'>Bem-vindo ao SEGUES!</h2>" +
                    $"<p>Use o código abaixo para criar a sua conta (expira em 5 minutos):</p>" +
                    $"<h1 style='background-color: #eee; padding: 10px; display: inline-block; letter-spacing: 5px;'>{verificationCode}</h1></div>");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email de verificação para {Email}", Input.Email);
                // Remove temporary data
                TempData.Remove("RegistrationData");
                ModelState.AddModelError(string.Empty, "Falha ao enviar o email. Verifique a sua conexão ou tente mais tarde.");
                return Page();
            }
            return RedirectToPage("VerifyCode", new { returnUrl });
        }
    }
}
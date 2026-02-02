using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Projeto_SEGUES.Models; // Confirma o teu namespace
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json; // Necessário para guardar os dados temporariamente
using System.Threading.Tasks;
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "O campo {0} é obrigatório.")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "O {0} deve ter no mínimo {2} letras.")]
            [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
            [Display(Name = "Primeiro Nome")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "O campo {0} é obrigatório.")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "O {0} deve ter no mínimo {2} letras.")]
            [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
            [Display(Name = "Sobrenome")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "O campo {0} é obrigatório.")]
            [Display(Name = "Género")]
            public Gender Gender { get; set; }

            [Required(ErrorMessage = "O campo {0} é obrigatório.")]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "O campo {0} é obrigatório.")]
            [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$",
                ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo.")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar password")]
            [Compare("Password", ErrorMessage = "A password e a confirmação não coincidem.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // 1. Verificar se o email já existe ANTES de enviar código
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Input.Email", "Este email já está registado.");
                    return Page();
                }

                // 2. Gerar Código de 6 dígitos
                Random generator = new Random();
                string verificationCode = generator.Next(100000, 999999).ToString();

               
                var tempData = new
                {
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    Gender = Input.Gender,
                    Email = Input.Email,
                    Password = Input.Password,
                    Code = verificationCode,

                    ExpiryTime = DateTime.Now.AddMinutes(5)
                };

              
                TempData["RegistrationData"] = JsonSerializer.Serialize(tempData);


                try
                {
                    // Tenta enviar o email
                    await _emailSender.SendEmailAsync(Input.Email, "Código de Validação SEGUES",
                        $"<h2 style='color: #2c3e50;'>Bem-vindo ao SEGUES!</h2>" +
                        $"<p>Use o código abaixo para criar a sua conta:</p>" +
                        $"<h1 style='background-color: #eee; padding: 10px; display: inline-block; letter-spacing: 5px;'>{verificationCode}</h1>");
                }
                catch (Exception ex)
                {
                    

                    // 1. Apaga os dados temporários porque o processo falhou
                    TempData.Remove("RegistrationData");

                    // 2. Adiciona uma mensagem de erro para o utilizador ver
                    ModelState.AddModelError(string.Empty, "Falha ao enviar o email. Verifique a sua conexão ou tente mais tarde.");

                    
                    return Page();
                }

                return RedirectToPage("VerifyCode");
            }

            return Page();
        }
    }
}
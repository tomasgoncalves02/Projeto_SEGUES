using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Identity.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model responsible for validating the verification code sent by email during registration.
    /// </summary>
    /// <remarks>
    /// This class manages the final confirmation of user data, automatic category assignment 
    /// (Student/IPS Worker/External), and the effective creation of the account in Identity.
    /// </remarks>
    public class VerifyCodeModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<VerifyCodeModel> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="VerifyCodeModel"/>.
        /// </summary>
        public VerifyCodeModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            AppDbContext context,
            IEmailSender emailSender,
            ILogger<VerifyCodeModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        /// <summary>
        /// Input model for the 6-digit code entered by the user.
        /// </summary>
        [BindProperty]
        public required InputModel Input { get; set; }

        /// <summary>
        /// User's email to be displayed in the interface for confirmation.
        /// </summary>
        public required string UserEmailDisplay { get; set; }

        /// <summary>
        /// Defines the validation structure for the code input.
        /// </summary>
        public class InputModel
        {
            /// <summary>Numeric verification code.</summary>
            [Required(ErrorMessage = "Introduza o código.")]
            public required string Code { get; init; }
        }

        /// <summary>
        /// Prepares the verification page, retrieving temporary registration data.
        /// </summary>
        /// <returns>The verification page or redirection to Registration if data expires.</returns>
        public IActionResult OnGet()
        {
            if (TempData["RegistrationData"] is not string) return RedirectToPage("Register");
            var data = TempData.GetJson<RegisterDataViewModel>("RegistrationData");
            if (data == null) return RedirectToPage("Register");

            UserEmailDisplay = data.Email;

            // Keep data in TempData for the next request (POST)
            TempData.Keep("RegistrationData");
            return Page();
        }

        /// <summary>
        /// Validates the entered code and creates the user account in the database.
        /// </summary>
        /// <remarks>
        /// This method performs business logic to classify the user based on the email suffix 
        /// and associates external logins if the flow was initiated by a provider (Google/Facebook).
        /// </remarks>
        public async Task<IActionResult> OnPostAsync()
        {
            if (TempData["RegistrationData"] is not string jsonData)
            {
                TempData.SetSwalError("Dados de registo expirados. Por favor, registe-se novamente.");
                return RedirectToPage("Register");
            }
            TempData.Keep("RegistrationData");

            var data = JsonSerializer.Deserialize<RegisterDataViewModel>(jsonData);
            if (data == null)
            {
                TempData.SetSwalError("Dados de registo expirados. Por favor, registe-se novamente.");
                return RedirectToPage("Register");
            }

            UserEmailDisplay = data.Email;

            if (!ModelState.IsValid) return Page();

            // Verify code expiration
            if (DateTime.Now > data.ExpiryTime)
            {
                TempData.Remove("RegistrationData");
                TempData.SetSwalError("O código expirou (limite de 5 minutos). Por favor registe-se novamente.");
                return RedirectToPage("Register");
            }

            if (Input.Code != data.Code)
            {
                ModelState.AddModelError("", "Código incorreto. Tente novamente.");
                return Page();
            }

            // Assign the user category based on email suffix
            string categoryName = "Externo";
            string emailLower = data.Email.ToLower();
            string? extractedStudentNumber = null;
            if (emailLower.EndsWith("@estudantes.ips.pt"))
            {
                categoryName = "Estudante";
                extractedStudentNumber = emailLower.Split('@')[0];
            }
            else if (emailLower.EndsWith("ips.pt"))
            {
                categoryName = "Trabalhador IPS";
            }

            var category = await _context.UserCategory.FirstOrDefaultAsync(c => c.Name == categoryName);

            AppUser user;
            
            // Map data to AppUser
            if (categoryName == "Estudante")
            {
                user = new Student
                {
                    UserName = data.Email,
                    Email = data.Email,
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    Gender = data.Gender,
                    BirthDate = data.BirthDate,
                    UserCategory = category!,
                    EmailConfirmed = true,
                    StudentNumber = extractedStudentNumber!
                };
            }
            else
            {
                user = new AppUser
                {
                    UserName = data.Email,
                    Email = data.Email,
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    Gender = data.Gender,
                    BirthDate = data.BirthDate,
                    UserCategory = category!,
                    EmailConfirmed = true
                };
            }

            var result = await _userManager.CreateAsync(user, data.Password);

            if (result.Succeeded)
            {
                // If applicable, link external login (Google/Facebook) to the newly created user
                if (TempData.TryGetValue("ExternalLoginProvider", out object? value))
                {
                    var provider = value?.ToString()!;
                    var key = TempData["ExternalLoginKey"]?.ToString()!;
                    var info = new UserLoginInfo(provider, key, provider);
                    await _userManager.AddLoginAsync(user, info);
                }

                await _userManager.AddToRoleAsync(user, "Client");
                await _signInManager.SignInAsync(user, isPersistent: false);

                TempData.Remove("RegistrationData");
                TempData.SetSwalSuccess("Conta criada e validada com sucesso!");
                _logger.LogAppUser($"User {user.Email} account created.", UserAction.Create);
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        /// <summary>
        /// Generates and sends a new verification code to the user.
        /// </summary>
        /// <returns>The current page with a resend confirmation message.</returns>
        public async Task<IActionResult> OnPostResendCodeAsync()
        {
            var data = TempData.GetJson<RegisterDataViewModel>("RegistrationData");
            if (data == null)
            {
                TempData.SetSwalError("Dados de registo expirados. Por favor, registe-se novamente.");
                return RedirectToPage("Register");
            }

            // Regenerate new code
            string newCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            data.Code = newCode;
            data.ExpiryTime = DateTime.Now.AddMinutes(5);
            TempData.SetJson("RegistrationData", data);

            var emailBody = ((EmailSender)_emailSender).GetEmailBody(
                "Código de Validação SEGUES",
                data.FirstName,
                $"""
                 <div style='text-align: center;'>
                    <p>Use o código abaixo (expira em 5 minutos):</p>
                    <h1 style='background-color: #eee; padding: 10px; display: inline-block; letter-spacing: 5px;'>{newCode}</h1>
                 </div>
                 """);

            try
            {
                await _emailSender.SendEmailAsync(data.Email, "Código de Validação SEGUES", emailBody);
                TempData.SetSwalSuccess("Um novo código foi enviado para o seu email.");
            }
            catch (Exception ex)
            {
                _logger.LogAppError(AppErrors.ResendEmailError, TableName.All, AppOperation.Other, ex);
                TempData.SetSwalError("Erro ao reenviar o email. Tente mais tarde.");
            }

            UserEmailDisplay = data.Email;
            TempData.Keep("RegistrationData");
            return Page();
        }
    }
}
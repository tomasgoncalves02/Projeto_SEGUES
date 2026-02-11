using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Identity.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class VerifyCodeModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public VerifyCodeModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            AppDbContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
        }

        // Identity
        [BindProperty]
        public required InputModel Input { get; set; }

        public required string UserEmailDisplay { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Introduza o código.")]
            public required string Code { get; init; }
        }
   
        public IActionResult OnGet()
        {
            if (TempData["RegistrationData"] is not string) return RedirectToPage("Register");
            var data = TempData.GetJson<RegisterDataViewModel>("RegistrationData");
            if (data == null) return RedirectToPage("Register");
            UserEmailDisplay = data.Email;
            TempData.Keep("RegistrationData");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (TempData["RegistrationData"] is not string jsonData)
            {
                return RedirectToPage("Register");
            }
            TempData.Keep("RegistrationData");

            var data = JsonSerializer.Deserialize<RegisterDataViewModel>(jsonData)!;
            UserEmailDisplay = data.Email;

            if (!ModelState.IsValid) return Page();
            
            if (DateTime.Now > data.ExpiryTime)
            {
                TempData.Remove("RegistrationData");
                ModelState.AddModelError("", "O código expirou (limite de 5 minutos). Por favor registe-se novamente.");
                return Page();
            }
            if (Input.Code != data.Code)
            {
                ModelState.AddModelError("", "Código incorreto. Tente novamente.");
                return Page();
            }

            string categoryName = "Externo";
            if (data.Email.ToLower().Contains("@estudantes."))
            {
                categoryName = "Estudante";
            }
            else if (data.Email.ToLower().Contains("@ips.pt"))
            {
                categoryName = "Trabalhador IPS";
            }
            
            var category = await _context.UserCategories.FirstOrDefaultAsync(c => c.Name == categoryName);
            var user = new AppUser
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
            var result = await _userManager.CreateAsync(user, data.Password);

            if (result.Succeeded)
            {
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
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
        
        public async Task<IActionResult> OnPostResendCodeAsync()
        {
            var data = TempData.GetJson<RegisterDataViewModel>("RegistrationData");
            if (data == null) return RedirectToPage("Register");

            // Generate new code and update expiry
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
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro ao reenviar o email. Tente mais tarde.");
            }
            UserEmailDisplay = data.Email;
            TempData.Keep("RegistrationData");
            return Page();
        }
    }
}
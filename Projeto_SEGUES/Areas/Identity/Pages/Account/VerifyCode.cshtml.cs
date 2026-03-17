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
    /// <summary>
    /// Model responsável pela validação do código de verificação enviado por email durante o registo.
    /// </summary>
    /// <remarks>
    /// Esta classe gere a confirmação final dos dados do utilizador, a atribuição automática 
    /// de categorias (Estudante/Trabalhador IPS/Externo) e a criação efetiva da conta no Identity.
    /// </remarks>
    public class VerifyCodeModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="VerifyCodeModel"/>.
        /// </summary>
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

        /// <summary>
        /// Modelo de entrada para o código de 6 dígitos introduzido pelo utilizador.
        /// </summary>
        [BindProperty]
        public required InputModel Input { get; set; }

        /// <summary>
        /// Email do utilizador a ser exibido na interface para confirmação.
        /// </summary>
        public required string UserEmailDisplay { get; set; }

        /// <summary>
        /// Define a estrutura de validação para a introdução do código.
        /// </summary>
        public class InputModel
        {
            /// <summary>Código numérico de verificação.</summary>
            [Required(ErrorMessage = "Introduza o código.")]
            public required string Code { get; init; }
        }

        /// <summary>
        /// Prepara a página de verificação, recuperando os dados temporários do registo.
        /// </summary>
        /// <returns>A página de verificação ou redirecionamento para o Registo se os dados expirarem.</returns>
        public IActionResult OnGet()
        {
            if (TempData["RegistrationData"] is not string) return RedirectToPage("Register");
            var data = TempData.GetJson<RegisterDataViewModel>("RegistrationData");
            if (data == null) return RedirectToPage("Register");

            UserEmailDisplay = data.Email;
            // Mantém os dados no TempData para o próximo pedido (POST)
            TempData.Keep("RegistrationData");
            return Page();
        }

        /// <summary>
        /// Valida o código introduzido e cria a conta do utilizador na base de dados.
        /// </summary>
        /// <remarks>
        /// Este método realiza a lógica de negócio de classificar o utilizador com base no sufixo do email
        /// e associa logins externos caso o fluxo tenha sido iniciado por um provider (Google/Facebook).
        /// </remarks>
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

            // Verificação de expiração do código (5 minutos)
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

            // Lógica de Atribuição de Categoria Automática
            string categoryName = "Externo";
            if (data.Email.ToLower().Contains("@estudantes."))
            {
                categoryName = "Estudante";
            }
            else if (data.Email.ToLower().Contains("@ips.pt"))
            {
                categoryName = "Trabalhador IPS";
            }

            var category = await _context.UserCategory.FirstOrDefaultAsync(c => c.Name == categoryName);

            // Mapeamento dos dados temporários para a entidade AppUser
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
                // Vinculação de Login Externo (se aplicável)
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

        /// <summary>
        /// Gera e envia um novo código de verificação para o utilizador.
        /// </summary>
        /// <returns>A página atual com uma mensagem de confirmação de reenvio.</returns>
        public async Task<IActionResult> OnPostResendCodeAsync()
        {
            var data = TempData.GetJson<RegisterDataViewModel>("RegistrationData");
            if (data == null) return RedirectToPage("Register");

            // Regeneração do código e atualização da validade
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
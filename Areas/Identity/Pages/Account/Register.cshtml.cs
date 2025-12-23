// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Projeto_SEGUES.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using static Projeto_SEGUES.Models.Enums.Enums; // Para aceder aos Enums

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Primeiro Nome")]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Sobrenome")]
            public string LastName { get; set; }

            [Required]
            [Display(Name = "Género")]
            public Gender Gender { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
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
                var user = CreateUser();

                // ============================================================
                // LÓGICA DE ROLES (Definir antes de criar o user)
                // ============================================================
                string roleNameString = "ExternalEmployee"; // Valor por defeito
                UserRole roleEnum = UserRole.ExternalEmployee; // Valor por defeito

                // Verifica se é estudante
                if (Input.Email.ToLower().Contains("@estudantes."))
                {
                    roleNameString = "Student"; // Nome da Role para o Identity
                    roleEnum = UserRole.Student; // Nome do Enum da tua Classe
                }

                // ============================================================
                // PREENCHIMENTO DE DADOS
                // ============================================================
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.Gender = Input.Gender;
                user.Balance = 0m;
                user.CreationDate = DateTime.Now;
                user.Status = UserStatus.Active;

                // Aqui definimos o Enum na base de dados do User
                user.Role = roleEnum;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // Cria o utilizador na BD
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Nova conta criada com sucesso.");

                    // ============================================================
                    // ATRIBUIÇÃO DA ROLE DO IDENTITY (Permissões de Login)
                    // ============================================================
                    // 1. Garante que a Role existe na BD, se não, cria-a
                    if (!await _roleManager.RoleExistsAsync(roleNameString))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(roleNameString));
                    }

                    // 2. Adiciona o utilizador à Role
                    await _userManager.AddToRoleAsync(user, roleNameString);

                    // ============================================================

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirme o seu email",
                        $"Por favor confirme a sua conta <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        // ============================================================
                        // REDIRECIONAMENTO INTELIGENTE (ATUALIZADO)
                        // ============================================================

                        // Se for estudante, vai para a área de estudante
                        if (roleNameString == "Student")
                        {
                            // Redireciona para o StudentController, ação Index
                            return RedirectToAction("Index", "Student");
                        }
                        // Se for funcionário externo
                        else
                        {
                            // Redireciona para o EmployeeController, ação Index
                            return RedirectToAction("Index", "ExternalEmployee");
                        }
                        // ============================================================
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}
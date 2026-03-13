// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<AppUser> _userManager;

        public LoginModel(SignInManager<AppUser> signInManager, ILogger<LoginModel> logger, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

        // Identity
        [BindProperty]
        public required InputModel Input { get; set; }

        // Identity
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        // Identity
        public string? ReturnUrl { get; set; }

        // Identity
        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Endereço de email")]
            public required string Email { get; init; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public required string Password { get; init; }

            [Display(Name = "Lembrar-me")]
            public bool RememberMe { get; init; }
        }

        // Identity
        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // 1. Procurar o utilizador pelo email
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user != null)
                {
                    // 2. Verificar se está desativado (usando o teu Enum)
                    if (user.Status == Projeto_SEGUES.Models.Enums.UserStatus.Inactive)
                    {
                        _logger.LogWarning("Tentativa de login em conta desativada: {Email}", Input.Email);
                        TempData.SetSwalError("A sua conta foi desativada pela administração.");
                        return Page();
                    }
                }

                // 3. Se passar o check, tenta o login normal
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    TempData.SetSwalSuccess("Login efetuado com sucesso!");
                    return LocalRedirect(returnUrl);
                }

                // 4. Verificação de Autenticação de Dois Fatores (2FA)
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = "/", RememberMe = Input.RememberMe });
                }

                // 5. Verificação de Bloqueio (Lockout)
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out due to failed attempts.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    
                    _logger.LogWarning("Invalid login attempt.");
                    TempData.SetSwalError("Tentativa de login inválida.");
                    return Page();
                }
            }
           
            return Page();
        }
    }
}
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

        public LoginModel(SignInManager<AppUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
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
                /* Email+Password only now. When adding external providers:
                 * https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins?view=aspnetcore-10.0
                 * Ensure password creation: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/?view=aspnetcore-10.0&tabs=visual-studio
                 */
                
                // Password failures trigger account lockout for 5 minutes after 5 failed attempts.
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    TempData.SetSwalSuccess( "Login efetuado com sucesso!");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2Fa", new { ReturnUrl = returnUrl, Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                TempData.SetSwalError("Tentativa de login inválida.");
                return Page();
            }
            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
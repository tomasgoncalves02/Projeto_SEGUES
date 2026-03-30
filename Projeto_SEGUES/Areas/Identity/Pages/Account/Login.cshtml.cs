// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account;

/// <summary>
/// Model responsible for the user authentication logic in the SEGUES system.
/// </summary>
/// <remarks>
/// Manages the local login process, external authentication (OAuth), account status verification (Active/Inactive), 
/// and security mechanisms such as failed attempt lockouts and 2FA.
/// </remarks>
public class LoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Initializes a new instance of <see cref="LoginModel"/>.
    /// </summary>
    public LoginModel(SignInManager<AppUser> signInManager, ILogger<LoginModel> logger, UserManager<AppUser> userManager)
    {
        _signInManager = signInManager;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Data input model for login credentials.
    /// </summary>
    [BindProperty]
    public required InputModel Input { get; set; }

    /// <summary>
    /// List of configured external authentication providers (e.g., Google).
    /// </summary>
    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    /// <summary>
    /// Redirect URL after successful authentication.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Stores temporary error messages from redirects.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Defines the structure and validations of the login form.
    /// </summary>
    public class InputModel
    {
        /// <summary>Unique user email identifier.</summary>
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Endereço de email inválido.")]
        [Display(Name = "Endereço de email")]
        public required string Email { get; init; }

        /// <summary>Access password.</summary>
        [Required(ErrorMessage = "A password é obrigatória.")]
        [StringLength(100, ErrorMessage = "A password deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 12)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
            ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo. E no mínimo 12 caracteres.")]
        [Display(Name = "Password")]
        public required string Password { get; init; }

        /// <summary>Defines if the authentication cookie should persist after closing the browser.</summary>
        [Display(Name = "Lembrar-me")]
        public bool RememberMe { get; init; }
    }

    /// <summary>
    /// Prepares the login page for display (GET).
    /// </summary>
    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError("", ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        ReturnUrl = returnUrl;
    }

    /// <summary>
    /// Processes the user authentication attempt (POST).
    /// </summary>
    /// <returns>Redirect in case of success or the page with errors in case of failure.</returns>
    /// <remarks>
    /// The workflow includes:
    /// 1. Verifying user existence.
    /// 2. Validating account status (administrative block if <see cref="UserStatus.Inactive"/>).
    /// 3. Credential verification via <see cref="SignInManager{TUser}.PasswordSignInAsync(string, string, bool, bool)"/>.
    /// </remarks>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
        {
            TempData.SetSwalError("Por favor corrija os erros no formulário.");
            return Page();
        }

        // Not allow login if the user is inactive
        var user = await _userManager.FindByEmailAsync(Input.Email);

        if (user is { Status: UserStatus.Inactive })
        {
            _logger.LogAppUser($"Login attempt in inactivated account: {Input.Email}", UserAction.LogIn);
            TempData.SetSwalError("A sua conta está desativada.");
            return Page();
        }

        // Identity Authentication
        var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogAppUser($"User {Input.Email} logged in successfully.", UserAction.LogIn);
            TempData.SetSwalSuccess("Login efetuado com sucesso!");
            return LocalRedirect(returnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = "/", Input.RememberMe });
        }

        if (result.IsLockedOut)
        {
            _logger.LogAppUser($"Account {Input.Email} locked out due to failed login attempts.", UserAction.LogIn);
            return RedirectToPage("./Lockout");
        }

        _logger.LogAppUser($"Failed login attempt for {Input.Email}.", UserAction.LogIn);
        TempData.SetSwalError("Tentativa de login inválida.");
        return Page();
    }
}
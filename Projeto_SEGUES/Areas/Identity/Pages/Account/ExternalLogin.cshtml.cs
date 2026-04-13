// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Areas.Identity.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Projeto_SEGUES.Attributes;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account;

/// <summary>
/// Model responsible for the authentication and registration flow through external providers.
/// </summary>
/// <remarks>
/// This model manages the provider callback, collection of additional user data, 
/// and the code verification flow before the final account creation.
/// </remarks>
[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ExternalLoginModel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ExternalLoginModel"/>.
    /// </summary>
    public ExternalLoginModel(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        ILogger<ExternalLoginModel> logger,
        IEmailSender emailSender)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _emailSender = emailSender;
    }

    /// <summary>
    /// Input data for finalizing external registration.
    /// </summary>
    [BindProperty]
    public required InputModel Input { get; set; }

    /// <summary>
    /// Display name of the authentication provider (e.g., Google).
    /// </summary>
    public string ProviderDisplayName { get; set; }

    /// <summary>
    /// Redirect URL after the login process.
    /// </summary>
    public string ReturnUrl { get; set; }

    /// <summary>
    /// Error message persisted between requests.
    /// </summary>
    [TempData]
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Defines the necessary properties to complete the user profile.
    /// </summary>
    public class InputModel
    {
        /// <summary>User email obtained from the provider.</summary>
        [Required]
        [EmailAddress]
        public required string Email { get; init; }

        /// <summary>User's first name.</summary>
        [Required]
        [Display(Name = "Primeiro Nome")]
        public string FirstName { get; init; }

        /// <summary>User's last name.</summary>
        [Required]
        [Display(Name = "Sobrenome")]
        public string LastName { get; init; }

        /// <summary>User's gender (as per <see cref="Gender"/>).</summary>
        [Required]
        [Display(Name = "Género")]
        public Gender Gender { get; init; }

        /// <summary>Date of birth with minimum age validation.</summary>
        [Required]
        [DataType(DataType.Date, ErrorMessage = "A data de nascimento deve ser uma data válida.")]
        [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos para se registrar.")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Nascimento")]
        public DateTime BirthDate { get; init; }
    }

    /// <summary>Redirects to login if access is direct via GET.</summary>
    public IActionResult OnGet() => RedirectToPage("./Login");

    /// <summary>Initiates the authentication challenge for the external provider.</summary>
    public IActionResult OnPost(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    /// <summary>Processes the return from the external provider and checks if the user already has an account.</summary>
    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
    {
        returnUrl ??= Url.Content("~/");
        if (remoteError != null)
        {
            ErrorMessage = $"Erro do autenticador externo: {remoteError}";
            TempData.SetSwalError(ErrorMessage);
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Erro obtendo informação de login do autenticador externo.";
            TempData.SetSwalError(ErrorMessage);
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name, info.LoginProvider);
            return LocalRedirect(returnUrl);
        }
        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        ReturnUrl = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName;
        if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
        {
            Input = new InputModel
            {
                Email = info.Principal.FindFirstValue(ClaimTypes.Email)
            };
        }
        return Page();
    }

    /// <summary>
    /// Validates submitted data and initiates the email code verification flow.
    /// </summary>
    /// <remarks>
    /// If the user already exists, it simply links the login. Otherwise, it generates a random 
    /// code and temporarily stores the data in TempData for subsequent validation.
    /// </remarks>
    public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user != null)
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            var verificationCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var registrationData = new RegisterDataViewModel
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Gender = Input.Gender,
                Email = Input.Email,
                BirthDate = Input.BirthDate,
                Password = Guid.NewGuid() + "1aA!",
                ConfirmPassword = "",
                Code = verificationCode,
                ExpiryTime = DateTime.Now.AddMinutes(5)
            };

            TempData.SetJson("RegistrationData", registrationData);
            TempData["ExternalLoginProvider"] = info.LoginProvider;
            TempData["ExternalLoginKey"] = info.ProviderKey;

            var emailBody = ((EmailSender)_emailSender).GetEmailBody(
                "Bem-vindo ao SEGUES!",
                Input.FirstName,
                $"""
                 <div style='text-align: center;'>
                    <p>Use o código abaixo para criar a sua conta (expira em 5 minutos):</p>
                    <h1 style='background-color: #eee; padding: 10px; display: inline-block; letter-spacing: 5px;'>{verificationCode}</h1>
                 </div>
                 """);
            await _emailSender.SendEmailAsync(Input.Email, "Código de Validação SEGUES", emailBody);
            return RedirectToPage("VerifyCode", new { returnUrl });
        }
        ProviderDisplayName = info.ProviderDisplayName;
        ReturnUrl = returnUrl;
        return Page();
    }
}
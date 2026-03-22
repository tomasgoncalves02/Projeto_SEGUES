using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account.Manage;

/// <summary>
/// Model class for the user password change page.
/// </summary>
/// <remarks>
/// This class manages the validation logic for current credentials and updates to a new 
/// password within the identity system, ensuring compliance with security requirements.
/// </remarks>
public class ChangePasswordModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<ChangePasswordModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordModel"/> class.
    /// </summary>
    /// <param name="userManager">Service for Identity user management.</param>
    /// <param name="signInManager">Service for authentication and session management.</param>
    /// <param name="logger">Service for event and error logging.</param>
    public ChangePasswordModel(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<ChangePasswordModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the data entry model for the form.
    /// </summary>
    [BindProperty]
    public required InputModel Input { get; set; }

    /// <summary>
    /// Defines the data structure and validation rules for password changes.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// The user's current password.
        /// </summary>
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 12)]
        [DataType(DataType.Password)]
        [Display(Name = "Password Actual")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
            ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo. E no mínimo 12 caracteres.")]
        public required string OldPassword { get; init; }

        /// <summary>
        /// The new password desired by the user.
        /// </summary>
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 12)]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
            ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo. E no mínimo 12 caracteres.")]
        public required string NewPassword { get; init; }

        /// <summary>
        /// Confirmation of the new password.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar password")]
        [Compare("NewPassword", ErrorMessage = "A password e a confirmação não coincidem.")]
        public required string ConfirmPassword { get; init; }
    }

    /// <summary>
    /// Processes the initial GET request for the password change page.
    /// </summary>
    /// <returns>The corresponding Razor page or an error if the user is not found.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogAppError(AppErrors.UserNotFound, TableName.All, AppOperation.Other);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.UserNotFound });
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);
        if (!hasPassword)
        {
            TempData.SetSwalWarning("A sua conta não tem uma password local definida.");
            return RedirectToPage("/Account/ResetPassword", new { Area = "Identity" });
        }

        return Page();
    }

    /// <summary>
    /// Processes the submission of the password change form.
    /// </summary>
    /// <returns>
    /// Redirection to the user profile on success or the current page with error messages on failure.
    /// </returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogAppError(AppErrors.UserNotFound, TableName.All, AppOperation.Other);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.UserNotFound });
        }

        var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            _logger.LogAppUser($"Failed attempt to change password for account {user.Email}.", UserAction.Update);

            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return Page();
        }

        // Refresh the user's authentication token to ensure they remain logged in after changing their password.
        await _signInManager.RefreshSignInAsync(user);
        _logger.LogAppUser($"User {user.Email} changed his password successfully.", UserAction.Update);

        TempData.SetSwalSuccess("A sua password foi alterada com sucesso.");
        return RedirectToAction("Index", "User", new { area = "User" });
    }
}
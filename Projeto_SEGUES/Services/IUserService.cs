using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Interface for User Profile Management Service.
/// Defines the methods required for users to view and update their personal information, 
/// school affiliation, and account settings.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user entity specifically prepared for profile editing.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The <see cref="AppUser"/> entity or null if not found.</returns>
    Task<AppUser?> GetUserForEditAsync(string userId);

    /// <summary>
    /// Retrieves a list of available schools/organic units for dropdown population.
    /// </summary>
    /// <returns>A list of <see cref="SelectListItem"/> containing school names and IDs.</returns>
    Task<List<SelectListItem>> GetSchoolsAsync();

    /// <summary>
    /// Validates and updates the user's profile information based on the provided view model.
    /// </summary>
    /// <param name="user">The current authenticated user entity.</param>
    /// <param name="model">The view model containing the updated profile data (Address, Phone, etc.).</param>
    /// <returns>A <see cref="ServiceResult"/> indicating the success or failure of the update.</returns>
    Task<ServiceResult> UpdateUserProfileAsync(AppUser user, EditUserViewModel model);
}
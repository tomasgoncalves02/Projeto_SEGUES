using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Interface for the Administrative Service of the SEGUES project.
/// Defines the required methods for user lifecycle, system configuration, 
/// auditing, and service management.
/// </summary>
public interface IAdminService
{
    #region Internal User Creation

    /// <summary>Creates a new internal account (Admin/Employee).</summary>
    /// <param name="model">The data for the new account.</param>
    /// <returns>A ServiceResult containing the operation outcome.</returns>
    Task<ServiceResult> CreateInternalUserAsync(CreateInternalUserViewModel model);

    #endregion

    #region User Management

    /// <summary>Retrieves users based on search strings and category/role filters.</summary>
    Task<List<UserDto>> GetFilteredUsersAsync(UserSearchViewModel? model = null);
    
    /// <summary>Gets a specific role entity by its name.</summary>
    Task<Role?> GetRoleByNameAsync(string roleName);
    
    /// <summary>Updates a user's full profile, balance, and role from the admin panel.</summary>
    Task<ServiceResult> UpdateUserAdminAsync(AppUser user, EditUserAdminViewModel model, IUrlHelper url, string scheme);
    
    /// <summary>Returns roles available for dropdowns, excluding the 'Client' role.</summary>
    Task<List<SelectListItem>> GetNonClientRolesForDropdownAsync();

    /// <summary>Returns all system roles for dropdown selection.</summary>
    Task<List<SelectListItem>> GetAllRolesForDropdownAsync();

    /// <summary>Returns all user categories for dropdown selection.</summary>
    Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync();

    /// <summary>Retrieves audited logs for staff members (Employee role).</summary>
    Task<List<StaffLogDto>> GetStaffLogFilteredAsync(string? searchString = null, UserAction? userAction = null, DateTime? dateFilter = null);

    #endregion

    #region Bar and Canteen Configuration

    /// <summary>Retrieves external links for bar and canteen menus.</summary>
    Task<BarCanteenConfigViewModel> GetMenuLinksAsync();

    /// <summary>Updates external menu links for both services.</summary>
    Task UpdateMenuLinksAsync(string? canteenLink, string? barLink);

    /// <summary>Retrieves the full operational schedule configuration.</summary>
    Task<BarCanteenConfigViewModel> GetScheduleAsync();

    /// <summary>Updates service opening and closing hours with range validation.</summary>
    Task<ServiceResult> UpdateScheduleAsync(BarCanteenConfigViewModel model);

    /// <summary>Checks if the bar service is currently operational.</summary>
    Task<bool> IsBarOpenAsync(TimeSpan? requestedTime);

    #endregion

    #region Ticket Management

    /// <summary>Retrieves the latest valid meal ticket prices per category.</summary>
    Task<List<TicketPrice>> GetTicketPricesAsync();

    /// <summary>Updates prices by record versioning (closing old, opening new).</summary>
    Task<ServiceResult> UpdateTicketPricesAsync(List<TicketPriceUpdateDto> prices);

    /// <summary>Gets the global validity duration for issued meal tickets.</summary>
    Task<int> GetTicketValidityDaysAsync();

    /// <summary>Updates the global validity duration for issued meal tickets.</summary>
    Task<ServiceResult> UpdateTicketValidityDaysAsync(int days);

    /// <summary>Toggles service availability for specific weekend days (Saturday/Sunday).</summary>
    Task<ServiceResult> UpdateSpecificDayStatusAsync(string day, bool isOpen);

    #endregion
}
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public interface IAdminService
{
    // Internal User Creation
    Task<ServiceResult> CreateInternalUserAsync(CreateInternalUserViewModel model);
    
    // User Management
    Task<List<UserDto>> GetFilteredUsersAsync(string? searchString = null, string? roleFilter = null, string? categoryFilter = null);
    Task<UserCategory?> GetCategoryByNameAsync(string modelCategory);
    Task RequestEmailChangeAsync(AppUser user, string newEmail, IUrlHelper urlHelper, string scheme);
    Task<List<SelectListItem>> GetNonClientRolesForDropdownAsync();
    Task<List<SelectListItem>> GetAllRolesForDropdownAsync();
    Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync();

    // Bar and Canteen Configuration
    Task<BarCanteenConfigViewModel> GetMenuLinksAsync();
    Task UpdateMenuLinksAsync(string? canteenLink, string? barLink);
    Task<BarCanteenConfigViewModel> GetScheduleAsync();
    Task<ServiceResult> UpdateScheduleAsync(BarCanteenConfigViewModel model);
    Task<bool> IsBarOpenAsync(TimeSpan? requestedTime);

    // Ticket Management
    Task<List<TicketPrice>> GetTicketPricesAsync();
    Task<ServiceResult> UpdateTicketPricesAsync(List<TicketPriceUpdateDto> prices);
    Task<int> GetTicketValidityDaysAsync();
    Task<ServiceResult> UpdateTicketValidityDaysAsync(int days);
    Task<ServiceResult> UpdateSpecificDayStatusAsync(string day, bool isOpen);
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Audit;
using Projeto_SEGUES.Models.Enums;
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
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task RequestEmailChangeAsync(AppUser user, string newEmail, IUrlHelper urlHelper, string scheme);
    Task<ServiceResult> UpdateUserAdminAsync(AppUser user, EditUserAdminViewModel model, IUrlHelper url, string scheme);
    Task<List<SelectListItem>> GetNonClientRolesForDropdownAsync();
    Task<List<SelectListItem>> GetAllRolesForDropdownAsync();
    Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync();
    Task<List<StaffLogDto>> GetStaffLogFilteredAsync(string? searchString = null, UserAction? userAction = null, DateTime? dateFilter = null);

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
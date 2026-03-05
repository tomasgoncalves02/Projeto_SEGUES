using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Areas.Admin.ViewModels;

namespace Projeto_SEGUES.Services;

public interface IAdminService
{
    // CreateInternalUser
    Task<IdentityResult> CreateInternalUserAsync(CreateInternalUserViewModel model);
    Task<List<SelectListItem>> GetNonClientRolesForDropdownAsync();
    Task<List<SelectListItem>> GetAllRolesForDropdownAsync();
    Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync();
    
    // User Management
    Task<List<AppUser>> GetFilteredUsersAsync(string? searchString, string? roleFilter, string? categoryFilter);
    Task<UserCategory> GetCategoryByNameAsync(string modelCategory);
    
    // Ticket Management
    Task<List<TicketPrice>> GetTicketPricesAsync();
    Task UpdateTicketPricesAsync(List<TicketPrice> prices);
    Task<int> GetTicketValidityDaysAsync();
    Task UpdateTicketValidityDaysAsync(int days);

    Task<TimeSpan> GetOpenBarTimeAsync();
    Task<TimeSpan> GetCloseBarTimesAsync();

    Task UpdateBarScheduleAsync(string openBarTime, string closeBarTime);



}
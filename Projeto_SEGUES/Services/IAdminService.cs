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
    Task<List<SelectListItem>> GetRolesForDropdownAsync();
    
    // User Management
    Task<List<AppUser>> GetFilteredUsersAsync(string roleFilter, string searchString);
    
    // Ticket Management
    Task<List<TicketPrice>> GetTicketPricesAsync();
    Task UpdateTicketPricesAsync(List<TicketPrice> prices);
}
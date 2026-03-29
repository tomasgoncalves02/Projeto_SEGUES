using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public interface IUserService
{
    Task<AppUser?> GetUserForEditAsync(string userId);
    Task<List<SelectListItem>> GetSchoolsAsync();
    Task<ServiceResult> UpdateUserProfileAsync(AppUser user, EditUserViewModel model);
}
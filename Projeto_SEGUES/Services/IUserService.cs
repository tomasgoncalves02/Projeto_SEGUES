using Microsoft.AspNetCore.Mvc.Rendering;

namespace Projeto_SEGUES.Services;

public interface IUserService
{
    List<SelectListItem> GetAllGendersForDropdownAsync();
}
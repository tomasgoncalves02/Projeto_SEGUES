using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    
    public UserService(AppDbContext context) => _context = context;

    public List<SelectListItem> GetAllGendersForDropdownAsync()
    {
        var genders = Enum.GetValues<Gender>()
            .Select(g => new SelectListItem
            {
                Value = g.ToString(),
                Text = g.ToDisplayName()
            })
            .ToList();
        return genders;
    }
}
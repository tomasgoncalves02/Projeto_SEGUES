using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel used for searching and filtering users within the administrative area.
/// </summary>
public class UserSearchViewModel
{
    /// <summary>
    /// Search string used to filter users by name or email.
    /// </summary>
    public string? SearchString { get; set; }
    
    /// <summary>
    /// Role filter used to narrow down the search results.
    /// </summary>
    public string? RoleFilter { get; set; }
    
    /// <summary>
    /// Category filter used to further refine the search results.
    /// </summary>
    public string? CategoryFilter { get; set; }
    
    /// <summary>
    /// The collection of user entities that match the specified search and filter criteria.
    /// </summary>
    public IEnumerable<UserDto> Results { get; set; } = new List<UserDto>();
}
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel used for the administrative management of user accounts.
/// </summary>
public class AdminUserManagementViewModel
{
    /// <summary>
    /// List of roles and categories available for selection.
    /// </summary>
    public List<SelectListItem> Roles { get; set; } = [];
    
    /// <summary>
    /// List of categories used to filter the product display in the UI.
    /// </summary>
    public List<SelectListItem> Categories { get; set; } = [];
    
    /// <summary>
    /// Nested ViewModel used to capture data for searching and filtering users.
    /// </summary>
    public UserSearchViewModel SearchModel { get; set; } = new();
}
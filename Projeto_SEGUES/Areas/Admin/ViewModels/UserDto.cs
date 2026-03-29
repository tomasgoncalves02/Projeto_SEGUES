namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// Data Transfer Object (DTO) for User models.
/// </summary>
public class UserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; set; } = "";
    
    /// <summary>
    /// User's full name'
    /// </summary>
    public string FullName { get; set; } = "";
    
    /// <summary>
    /// User's initial
    /// </summary>
    public string Initial { get; set; } = "?";
    
    /// <summary>
    /// User's email
    /// </summary>
    public string Email { get; set; } = "";
    
    /// <summary>
    /// User's role
    /// </summary>
    public string RoleName { get; set; } = "";
    
    /// <summary>
    /// Badge class for the user's role
    /// </summary>
    public string RoleBadgeClass { get; set; } = "";
    
    /// <summary>
    /// User's category
    /// </summary>
    public string CategoryName { get; set; } = "";
    
    /// <summary>
    /// Badge class for the user's category
    /// </summary>
    public string CategoryBadgeClass { get; set; } = "";
    
    /// <summary>
    /// User's account active status
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// User's balance formatted as a currency string
    /// </summary>
    public string BalanceFormatted { get; set; } = "";
}
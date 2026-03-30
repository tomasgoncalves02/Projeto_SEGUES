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
    public string Initial => FullName.Substring(0, 1).ToUpper();
    
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
    /// User's account status formatted for display
    /// </summary>
    public string StatusDisplay => IsActive ? "Ativo" : "Inativo";
    
    /// <summary>
    /// Badge class and icon for the user's account status
    /// </summary>
    public string StatusBadgeClass => IsActive ? "bg-success" : "bg-danger";
    
    /// <summary>
    /// Icon for the user's account status
    /// </summary>
    public string StatusIcon => IsActive ? "bi-check-circle" : "bi-x-circle";
    
    /// <summary>
    /// User's balance formatted as a currency string
    /// </summary>
    public string BalanceFormatted { get; set; } = "";
    
    /// <summary>
    /// User's gender for UI
    /// </summary>
    public string GenderDisplay { get; set; } = "";
    
    /// <summary>
    /// User's birthdate formatted as a string
    /// </summary>
    public string BirthDateDisplay { get; set; } = "";
    
    /// <summary>
    /// User's creation date formatted as a string
    /// </summary>
    public string CreationDateDisplay { get; set; } = "";
}
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

/// <summary>
/// Custom Identity role entity extending <see cref="IdentityRole"/>.
/// </summary>
/// <remarks>
/// This model provides a mapping between technical normalized role names used in 
/// authorization attributes and the human-readable names shown in management dashboards.
/// </remarks>
public class Role : IdentityRole
{
    /// <summary>
    /// The localized, friendly name of the role (e.g., "Funcionário de Bar", "Gestor de Inventário").
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "O nome de exibição deve ter no máximo {1} caracteres.")]
    [Display(Name = "Nome de Exibição")]
    public required string DisplayName { get; set; }
}
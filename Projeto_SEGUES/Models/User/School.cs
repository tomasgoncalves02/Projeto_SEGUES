using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

/// <summary>
/// Entity representing an educational institution or specific campus unit within the system.
/// </summary>
/// <remarks>
/// This model enables the multi-tenant-like behavior of the platform, where 
/// <see cref="Employee"/> and <see cref="Student"/> entities are associated with 
/// a specific school for localized service management.
/// </remarks>
public class School
{
    /// <summary>Unique identifier for the school record.</summary>
    public int Id { get; set; }

    /// <summary>The full official name of the institution (e.g., "Escola Secundária de Casquilhos").</summary>
    [Required]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
    [Display(Name = "Nome")]
    public required string Name { get; set; }

    /// <summary>A unique short-hand identifier or acronym (e.g., "ESC", "ESS").</summary>
    [Required]
    [MaxLength(9, ErrorMessage = "O código deve ter no máximo {1} caracteres.")]
    [Display(Name = "Código/Sigla")]
    public required string Code { get; set; }

    /// <summary>The physical street address of the campus.</summary>
    [Required]
    [MaxLength(250, ErrorMessage = "Endereço deve ter no máximo {1} caracteres.")]
    [Display(Name = "Endereço")]
    public required string Address { get; set; }

    /// <summary>The city where the school is located.</summary>
    [Required]
    [MaxLength(50, ErrorMessage = "Cidade deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cidade")]
    public required string City { get; set; }

    /// <summary>Navigation property for geographic normalization and regional reporting.</summary>
    public PostalCode? PostalCode { get; set; }

    /// <summary>Toggle for active status; disabled schools are hidden from new registrations.</summary>
    public bool IsActive { get; set; } = true;
}
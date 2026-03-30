using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

/// <summary>
/// Entity representing a unique postal code within the regional infrastructure.
/// </summary>
/// <remarks>
/// This model is used to normalize address data, preventing redundant string storage 
/// and facilitating demographic reporting by geographic area.
/// </remarks>
public class PostalCode
{
    /// <summary>Unique identifier for the postal code record.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>The alphanumeric postal code (e.g., "2900-000").</summary>
    [Required]
    [MaxLength(9, ErrorMessage = "O código postal deve ter no máximo {1} caracteres.")]
    [Display(Name = "Código Postal")]
    public required string Code { get; set; }

    /// <summary>Collection of users residing within this specific postal code area.</summary>
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
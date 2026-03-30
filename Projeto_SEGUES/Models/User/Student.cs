using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

/// <summary>
/// Specialized user entity representing the student body of an institution.
/// </summary>
/// <remarks>
/// This model extends <see cref="AppUser"/> using Table-Per-Type (TPT) inheritance. 
/// It captures academic-specific identifiers and institutional affiliation, 
/// which are used for ticket eligibility and school-specific inventory access.
/// </remarks>
public class Student : AppUser
{
    /// <summary>
    /// The unique institutional identifier for the student (e.g., "A2024001").
    /// </summary>
    [MaxLength(20, ErrorMessage = "O número de estudante deve ter no máximo {1} caracteres.")]
    [Display(Name = "Número de Estudante")]
    public string? StudentNumber { get; set; }

    /// <summary>
    /// Navigation property to the school where the student is currently enrolled.
    /// </summary>
    public School? School { get; set; } // FK
}
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

/// <summary>
/// Specialized user entity representing staff members, teachers, and administrators.
/// </summary>
/// <remarks>
/// As a subclass of <see cref="AppUser"/>, this model inherits all identity and 
/// profile attributes while adding job-specific details and institutional affiliation.
/// </remarks>
public class Employee : AppUser
{
    /// <summary>
    /// A descriptive title of the employee's professional function (e.g., "Professor", "Cozinheiro").
    /// </summary>
    [MaxLength(100, ErrorMessage = "Cargo deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cargo")]
    public string? RoleDescription { get; set; }

    /// <summary>
    /// Navigation property to the specific school or campus unit where the employee is based.
    /// </summary>
    public School? School { get; set; } // FK
}
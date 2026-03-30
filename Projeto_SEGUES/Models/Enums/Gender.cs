using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Categorization of user gender for demographic profiles and identity management.
/// </summary>
/// <remarks>
/// This enum is utilized by the <see cref="AppUser"/> and <see cref="Student"/> models 
/// to ensure data consistency across the platform's registration and reporting modules.
/// Inherits from <see cref="byte"/> to optimize database storage.
/// </remarks>
public enum Gender : byte
{
    /// <summary>Represents male-identifying users.</summary>
    [Display(Name = "Masculino")]
    Male,

    /// <summary>Represents female-identifying users.</summary>
    [Display(Name = "Feminino")]
    Female,

    /// <summary>Represents users with other gender identities or those who prefer not to specify.</summary>
    [Display(Name = "Outro")]
    Other
}
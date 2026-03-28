using Microsoft.AspNetCore.Identity;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Validators;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

/// <summary>
/// Custom Identity user entity extending <see cref="IdentityUser"/>.
/// </summary>
/// <remarks>
/// This model acts as the base for all personas (Student, Employee, Admin). 
/// It integrates standard authentication (email, phone, password) with 
/// business-specific fields like <see cref="Balance"/> and <see cref="UserCategory"/>.
/// </remarks>
public class AppUser : IdentityUser
{
    /// <summary>The user's given name(s).</summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Primeiro(s) Nome(s)")]
    public required string FirstName { get; set; }

    /// <summary>The user's family name(s) or surname.</summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Apelido(s)")]
    public required string LastName { get; set; }

    /// <summary>The institutional category (e.g., Student, Teacher) used for pricing logic.</summary>
    [Required]
    public required UserCategory UserCategory { get; set; } // FK

    /// <summary>The current funds available in the user's digital wallet.</summary>
    [Range(0, double.MaxValue)]
    [Display(Name = "Saldo")]
    public decimal Balance { get; set; } // 0 is default

    /// <summary>Portuguese Tax Identification Number (NIF).</summary>
    [MaxLength(9, ErrorMessage = "O NIF deve ter no máximo {1} caracteres.")]
    [Display(Name = "NIF")]
    public string? FiscalNumber { get; set; }

    /// <summary>The user's date of birth, subject to age verification via <see cref="MinimumAgeAttribute"/>.</summary>
    [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
    [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos para se registrar.")]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Nascimento")]
    public required DateTime BirthDate { get; set; }

    /// <summary>User's gender for demographic reporting.</summary>
    [Required]
    public required Gender Gender { get; set; }

    /// <summary>The timestamp when the account was first created.</summary>
    [Display(Name = "Data de Criação")]
    public DateTime CreationDate { get; init; } = DateTime.Now;

    /// <summary>The current operational state of the account (Active, Inactive, Suspended).</summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>Street address and house number.</summary>
    [MaxLength(250, ErrorMessage = "Morada deve ter no máximo {1} caracteres.")]
    [Display(Name = "Morada")]
    public string? Address { get; set; }

    /// <summary>Name of the city or municipality.</summary>
    [MaxLength(50, ErrorMessage = "Cidade deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cidade")]
    public string? City { get; set; }

    /// <summary>Navigation property for postal code infrastructure and city mapping.</summary>
    public PostalCode? PostalCode { get; set; } // FK
}
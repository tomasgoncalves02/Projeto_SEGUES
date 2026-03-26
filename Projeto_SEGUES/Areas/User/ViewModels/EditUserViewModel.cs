using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Projeto_SEGUES.Attributes;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Validators;

namespace Projeto_SEGUES.Areas.User.ViewModels;

/// <summary>
/// ViewModel used by authenticated users to update their own profile information.
/// </summary>
/// <remarks>
/// This model includes validation for personal data and uses [ValidateNever] 
/// for attributes that are displayed in the UI but cannot be modified by the user.
/// </remarks>
public class EditUserViewModel
{
    /// <summary>Unique identifier for the user.</summary>
    public required string Id { get; init; }

    /// <summary>User's first name.</summary>
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
    [Display(Name = "Primeiro Nome")]
    public required string FirstName { get; set; }

    /// <summary>User's last name.</summary>
    [Required(ErrorMessage = "O sobrenome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
    [Display(Name = "Sobrenome")]
    public required string LastName { get; set; }

    /// <summary>Email address, displayed for reference but typically immutable in this view.</summary>
    [EmailAddress]
    [Display(Name = "Endereço de Email")]
    [ValidateNever]
    public string Email { get; set; }

    /// <summary>User's gender identification.</summary>
    [Required(ErrorMessage = "O género é obrigatório.")]
    [Display(Name = "Género")]
    public required Gender Gender { get; set; }

    /// <summary>Date of birth with age-based domain validation.</summary>
    [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
    [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
    [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos.")]
    [Display(Name = "Data de Nascimento")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [MaximumAge(120, ErrorMessage = "{0} inválida. Não pode ter mais de {1} anos.")]
    public required DateTime BirthDate { get; set; }

    /// <summary>Identity role assigned to the user, not modifiable by the user themselves.</summary>
    [Display(Name = "Tipo de Conta")]
    [ValidateNever]
    public Role Role { get; set; }

    /// <summary>Categorization string (e.g., Student, Employee), managed by system logic.</summary>
    [Display(Name = "Categoria de Utilizador")]
    [ValidateNever]
    public string Category { get; set; }

    /// <summary>Tax identification number (NIF).</summary>
    [MaxLength(9, ErrorMessage = "O NIF deve ter no máximo {1} caracteres.")]
    [Display(Name = "NIF")]
    public string? FiscalNumber { get; set; }

    /// <summary>Residential address details.</summary>
    [MaxLength(250, ErrorMessage = "Morada deve ter no máximo {1} caracteres.")]
    [Display(Name = "Morada")]
    public string? Address { get; set; }

    /// <summary>City of residence.</summary>
    [MaxLength(50, ErrorMessage = "Cidade deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cidade")]
    public string? City { get; set; }

    /// <summary>Formatted postal code.</summary>
    [MaxLength(9, ErrorMessage = "O Código Postal deve ter no máximo {1} caracteres.")]
    [Display(Name = "Código Postal")]
    public string? PostalCode { get; set; }

    /// <summary>Academic identifier for users categorized as Students.</summary>
    [MaxLength(20, ErrorMessage = "O Número de Estudante deve ter no máximo {1} caracteres.")]
    [Display(Name = "Número de Estudante")]
    public string? StudentNumber { get; set; }

    /// <summary>Professional role description for users categorized as Employees.</summary>
    [MaxLength(100, ErrorMessage = "O Cargo deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cargo")]
    public string? RoleDescription { get; set; }

    /// <summary>Identifier for the school the user is affiliated with.</summary>
    [Display(Name = "Escola")]
    public int? SchoolId { get; set; }
}
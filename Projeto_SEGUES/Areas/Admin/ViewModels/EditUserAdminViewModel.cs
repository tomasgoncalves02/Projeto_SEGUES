using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Attributes;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Validators;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel used by administrators to modify any user's profile and system settings.
/// </summary>
/// <remarks>
/// Unlike the standard user profile model, this version includes administrative 
/// fields such as account balance, role assignment, and user category.
/// </remarks>
public class EditUserAdminViewModel
{
    /// <summary>Unique identifier of the user (GUID from Identity).</summary>
    public required string Id { get; init; }

    /// <summary>User's first name.</summary>
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
    [Display(Name = "Primeiro Nome")]
    public required string FirstName { get; set; }

    /// <summary>User's last name or surname.</summary>
    [Required(ErrorMessage = "O sobrenome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
    [Display(Name = "Sobrenome")]
    public required string LastName { get; set; }

    /// <summary>Official email address and primary login identifier.</summary>
    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [Display(Name = "Endereço de Email")]
    public string Email { get; set; }

    /// <summary>User's gender identification.</summary>
    [Required(ErrorMessage = "O género é obrigatório.")]
    [Display(Name = "Género")]
    public required Gender Gender { get; set; }

    /// <summary>User's birth date, subject to age validation logic.</summary>
    [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
    [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
    [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos.")]
    [Display(Name = "Data de Nascimento")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [MaximumAge(120, ErrorMessage = "{0} inválida. Não pode ter mais de {1} anos.")]
    public required DateTime BirthDate { get; set; }

    /// <summary>Current monetary balance of the user's wallet.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "O saldo não pode ser negativo.")]
    [Display(Name = "Saldo (€)")]
    public decimal Balance { get; set; }

    /// <summary>The Identity role assigned to the user (e.g., Admin, Employee, Client).</summary>
    [Required(ErrorMessage = "Selecione o tipo de conta.")]
    [Display(Name = "Tipo de Conta")]
    public required string Role { get; set; }

    /// <summary>The specific user category (e.g., Student, IPS Worker, External).</summary>
    [Required(ErrorMessage = "A categoria de utilizador é obrigatória.")]
    [Display(Name = "Categoria de Utilizador")]
    public required string Category { get; set; }

    /// <summary>Tax identification number (NIF).</summary>
    [MaxLength(9, ErrorMessage = "O NIF deve ter no máximo {1} caracteres.")]
    [Display(Name = "NIF")]
    public string? FiscalNumber { get; set; }

    /// <summary>Street address and house number.</summary>
    [MaxLength(250, ErrorMessage = "Morada deve ter no máximo {1} caracteres.")]
    [Display(Name = "Morada")]
    public string? Address { get; set; }

    /// <summary>City or residential area.</summary>
    [MaxLength(50, ErrorMessage = "Cidade deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cidade")]
    public string? City { get; set; }

    /// <summary>Formatted postal code (e.g., 2900-000).</summary>
    [MaxLength(9, ErrorMessage = "O código postal deve ter no máximo {1} caracteres.")]
    [Display(Name = "Código Postal")]
    public string? PostalCode { get; set; }

    /// <summary>Specific identifier for Student type users.</summary>
    [MaxLength(20, ErrorMessage = "O número de estudante deve ter no máximo {1} caracteres.")]
    [Display(Name = "Número de Estudante")]
    public string? StudentNumber { get; set; }

    /// <summary>Job title or role description for Employee type users.</summary>
    [MaxLength(100, ErrorMessage = "Cargo deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cargo")]
    public string? RoleDescription { get; set; }

    /// <summary>Identifier for the school the user is affiliated with.</summary>
    [Display(Name = "Escola")]
    public int? SchoolId { get; set; }
    
    /// <summary>
    /// List of available roles for the user.
    /// </summary>
    [ValidateNever]
    public List<SelectListItem> RolesList { get; set; } = [];
    
    /// <summary>
    /// List of available categories for the user.
    /// </summary>
    [ValidateNever]
    public List<SelectListItem> CategoriesList { get; set; } = [];
    
    /// <summary>
    /// List of available schools for the user.
    /// </summary>
    [ValidateNever]
    public List<SelectListItem> SchoolsList { get; set; } = [];
    
    [ValidateNever]
    public bool IsStudent => Category.Equals("Estudante", StringComparison.OrdinalIgnoreCase);
    
    [ValidateNever]
    public bool IsEmployee => Role.Equals("Employee", StringComparison.OrdinalIgnoreCase);
    
    [ValidateNever]
    public bool ShowSchool => IsEmployee || Role.Equals("Client", StringComparison.OrdinalIgnoreCase);
}
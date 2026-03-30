using Projeto_SEGUES.Attributes;
using Projeto_SEGUES.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel used for registering new internal users (Staff/Admin).
/// </summary>
/// <remarks>
/// Contains all necessary properties for account creation and their respective 
/// integrity validations, such as character limits, regular expressions for names, and minimum age.
/// </remarks>
public class CreateInternalUserViewModel
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    /// <value>Required, between 2 and 50 characters, letters only.</value>
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
    [Display(Name = "Primeiro Nome")]
    public required string FirstName { get; init; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    /// <value>Required, between 2 and 50 characters, letters only.</value>
    [Required(ErrorMessage = "O sobrenome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
    [Display(Name = "Sobrenome")]
    public required string LastName { get; init; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    /// <value>Must be a valid email format and is required.</value>
    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [Display(Name = "Email")]
    public required string Email { get; init; }

    /// <summary>
    /// Gets or sets the user's gender.
    /// </summary>
    /// <value>Value from the <see cref="Gender"/> Enum.</value>
    [Required(ErrorMessage = "Selecione o género.")]
    [Display(Name = "Género")]
    public required Gender Gender { get; init; }

    /// <summary>
    /// Gets or sets the user's date of birth.
    /// </summary>
    /// <value>Required date. Requires a minimum age of 18 and a maximum of 120.</value>
    [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
    [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
    [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos para se registrar.")]
    [Display(Name = "Data de Nascimento")]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [MaximumAge(120, ErrorMessage = "{0} inválida. Não pode ter mais de {1} anos.")]
    public required DateTime BirthDate { get; init; }

    /// <summary>
    /// Gets or sets the account type (Role) to assign to the internal user.
    /// </summary>
    /// <value>Typically "Admin" or "Employee".</value>
    [Required(ErrorMessage = "Selecione o tipo de conta.")]
    [Display(Name = "Tipo de Conta")]
    public required string AccountType { get; init; }
}
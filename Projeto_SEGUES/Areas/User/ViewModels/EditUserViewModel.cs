using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Projeto_SEGUES.Attributes;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Validators;

namespace Projeto_SEGUES.Areas.User.ViewModels;

public class EditUserViewModel
{
    public required string Id { get; init; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
    [Display(Name = "Primeiro Nome")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "O sobrenome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
    [Display(Name = "Sobrenome")]
    public required string LastName { get; set; }
    
    [EmailAddress]
    [Display(Name = "Endereço de Email")]
    [ValidateNever]
    public string Email { get; set; }

    [Required(ErrorMessage = "O género é obrigatório.")]
    [Display(Name = "Género")]
    public required Gender Gender { get; set; }

    [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
    [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
    [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos.")]
    [Display(Name = "Data de Nascimento")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [MaximumAge(120, ErrorMessage = "{0} inválida. Não pode ter mais de {1} anos.")]
    public required DateTime BirthDate { get; set; }
    
    [Display(Name = "Tipo de Conta")]
    [ValidateNever]
    public Role Role { get; set; }
    
    [Display(Name = "Categoria de Utilizador")]
    [ValidateNever]
    public string Category { get; set; }

    [MaxLength(9, ErrorMessage = "O NIF deve ter no máximo {1} caracteres.")]
    [Display(Name = "NIF")]
    public string? FiscalNumber { get; set; }

    [MaxLength(250, ErrorMessage = "Morada deve ter no máximo {1} caracteres.")]
    [Display(Name = "Morada")]
    public string? Address { get; set; }

    [MaxLength(50, ErrorMessage = "Cidade deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cidade")]
    public string? City { get; set; }

    [MaxLength(9, ErrorMessage = "O Código Postal deve ter no máximo {1} caracteres.")]
    [Display(Name = "Código Postal")]
    public string? PostalCode { get; set; }

    [MaxLength(20, ErrorMessage = "O Número de Estudante deve ter no máximo {1} caracteres.")]
    [Display(Name = "Número de Estudante")]
    public string? StudentNumber { get; set; }

    [MaxLength(100, ErrorMessage = "O Cargo deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cargo")]
    public string? RoleDescription { get; set; }
    
    [Display(Name = "Escola")]
    public int? SchoolId { get; set; }
}
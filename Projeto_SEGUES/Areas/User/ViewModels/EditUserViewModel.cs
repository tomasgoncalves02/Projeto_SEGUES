using System.ComponentModel.DataAnnotations;
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
    
    [Range(0, double.MaxValue, ErrorMessage = "O saldo não pode ser negativo.")]
    [Display(Name = "Saldo (€)")]
    public decimal Balance { get; set; }
        
    [Required(ErrorMessage = "Selecione o tipo de conta.")]
    [Display(Name = "Tipo de Conta")]
    public required string Role { get; set; }
        
    [Required(ErrorMessage = "A categoria de utilizador é obrigatória.")]
    [Display(Name = "Categoria de Utilizador")]
    public required string Category { get; set; }
        
    [MaxLength(9)]
    [Display(Name = "NIF")]
    public string? FiscalNumber { get; set; }
        
    [MaxLength(250)]
    [Display(Name = "Morada")]
    public string? Address { get; set; }
        
    [MaxLength(50)]
    [Display(Name = "Cidade")]
    public string? City { get; set; }
        
    [MaxLength(9)]
    [Display(Name = "Código Postal")]
    public string? PostalCode { get; set; }
        
    [MaxLength(20)]
    [Display(Name = "Número de Estudante")]
    public string? StudentNumber { get; set; }
        
    [MaxLength(100)]
    [Display(Name = "Cargo")]
    public string? RoleDescription { get; set; }
        
    public School? School { get; set; }
}
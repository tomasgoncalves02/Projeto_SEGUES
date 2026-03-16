using Projeto_SEGUES.Attributes;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Validators;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Identity.ViewModels;

public class RegisterDataViewModel
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
    [Display(Name = "Primeiro Nome")]
    public required string FirstName { get; init; }
    
    [Required(ErrorMessage = "O sobrenome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
    [Display(Name = "Sobrenome")]
    public required string LastName { get; init; }
    
    [Required(ErrorMessage = "O género é obrigatório.")]
    [Display(Name = "Género")]
    public required Gender Gender { get; init; }
    
    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [Display(Name = "Email")]
    public required string Email { get; init; }
    
    [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
    [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
    [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos para se registrar.")]
    [Display(Name = "Data de Nascimento")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [MaximumAge(120, ErrorMessage = "{0} inválida. Não pode ter mais de {1} anos.")]
    public required DateTime BirthDate { get; init; }
    
    [Required(ErrorMessage = "A password é obrigatória.")]
    [StringLength(100, ErrorMessage = "A password deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 12)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
        ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo. E no mínimo 12 caracteres.")]
    public required string Password { get; set; }
    
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar password")]
    [Compare("Password", ErrorMessage = "A password e a confirmação não coincidem.")]
    public required string ConfirmPassword { get; set; }

    public required string Code { get; set; } = "";
    
    public required DateTime ExpiryTime { get; set; } = DateTime.Now;
}
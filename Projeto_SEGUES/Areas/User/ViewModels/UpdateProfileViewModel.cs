using Projeto_SEGUES.Attributes;
using Projeto_SEGUES.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.User.ViewModels;

public class UpdateProfileViewModel
{
    public required string Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "O sobrenome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "O género é obrigatório.")]
    public Gender Gender { get; set; }

    [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
    [MaximumAge(120, ErrorMessage = "Data inválida. Não pode ter mais de 120 anos.")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }
}
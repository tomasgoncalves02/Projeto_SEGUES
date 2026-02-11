using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Validators;

namespace Projeto_SEGUES.Areas.Admin.ViewModels
{
    public class CreateInternalUserViewModel
    {

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter no mínimo {2} letras.")]
        [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
        [Display(Name = "Primeiro Nome")]
        public string FirstName { get; init; }

        [Required(ErrorMessage = "O sobrenome é obrigatório.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter no mínimo {2} letras.")]
        [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
        [Display(Name = "Sobrenome")]
        public string LastName { get; init; }
        
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        public string Email { get; init; }

        [Required(ErrorMessage = "Selecione o género.")]
        [Display(Name = "Género")]
        public Gender Gender { get; init; }
        
        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        [DataType(DataType.Date)]
        [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos para se registrar.")]
        [Display(Name = "Data de Nascimento")]
        public DateTime BirthDate { get; init; }
        
        [Required(ErrorMessage = "Selecione o tipo de conta.")]
        [Display(Name = "Tipo de Conta")]
        public string AccountType { get; init; }
    }
}
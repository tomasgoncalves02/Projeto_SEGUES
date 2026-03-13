using Projeto_SEGUES.Attributes;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Validators;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Admin.ViewModels
{
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

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        [Display(Name = "Email")]
        public required string Email { get; set; }
        
        [Required(ErrorMessage = "Selecione o género.")]
        [Display(Name = "Género")]
        public required Gender Gender { get; set; }
        
        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
        [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos.")]
        [Display(Name = "Data de Nascimento")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [MaximumAge(120, ErrorMessage = "{0} inválida. Não pode ter mais de {1} anos.")]
        public required DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "O saldo é obrigatório.")]
        [Range(0, double.MaxValue, ErrorMessage = "O saldo não pode ser negativo.")]
        [Display(Name = "Saldo (€)")]
        public required decimal Balance { get; set; }
        
        [Required(ErrorMessage = "Selecione o tipo de conta.")]
        [Display(Name = "Tipo de Conta")]
        public required string Role { get; set; }
        
        [Required(ErrorMessage = "A categoria de utilizador é obrigatória.")]
        [Display(Name = "Categoria de Utilizador")]
        public required string Category { get; set; }
    }
}
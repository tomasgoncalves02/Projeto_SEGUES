using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "O {0} deve ter no mínimo {2} letras.")]
        [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
        [Display(Name = "Primeiro Nome")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "O {0} deve ter no mínimo {2} letras.")]
        [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
        [Display(Name = "Sobrenome")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [EmailAddress(ErrorMessage = "Introduza um email válido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [Range(0, double.MaxValue, ErrorMessage = "O saldo não pode ser negativo.")]
        [Display(Name = "Saldo (€)")]
        public decimal Balance { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [Display(Name = "Perfil (Role)")]
        public string Role { get; set; } 
    }
}
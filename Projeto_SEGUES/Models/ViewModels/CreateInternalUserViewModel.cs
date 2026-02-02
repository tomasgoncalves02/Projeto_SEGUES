using System.ComponentModel.DataAnnotations;
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Models
{
    public class CreateInternalUserViewModel
    {

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

        [RegularExpression(@"^[^@]*$", ErrorMessage = "Não escreva o '@' nem o email. Coloque apenas o nome (ex: rui.santos).")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [Display(Name = "Nome de Utilizador (ex: joaosilva)")]
        public string UsernameStub { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [Display(Name = "Tipo de Conta")]
        public string AccountType { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [Display(Name = "Género")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Introduza um email válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        // Validação de complexidade: 1 minúscula, 1 maiúscula, 1 número, 1 símbolo, min 6 chars
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$",
            ErrorMessage = "A password deve ter pelo menos 6 caracteres, incluindo: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data de Nascimento")]
        public DateTime? BirthDate { get; set; }
     
    }
}
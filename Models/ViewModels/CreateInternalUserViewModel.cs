using System.ComponentModel.DataAnnotations;
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Models
{
    public class CreateInternalUserViewModel
    {
        [Required]
        [Display(Name = "Primeiro Nome")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Sobrenome")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Nome de Utilizador (ex: joaosilva)")]
        public string UsernameStub { get; set; } // A parte antes do @

        [Required]
        [Display(Name = "Tipo de Conta")]
        public string AccountType { get; set; } // "Admin" ou "Funcionario"

        [Required]
        [Display(Name = "Género")]
        public Gender Gender { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
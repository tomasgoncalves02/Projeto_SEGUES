using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "Primeiro Nome")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Sobrenome")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Saldo (€)")]
        public decimal Balance { get; set; }

        [Display(Name = "Perfil (Role)")]
        public string Role { get; set; } // Admin, Employee, Student...
    }
}
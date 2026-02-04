using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User
{
    public class Student : User
    {
        [Required]
        [MaxLength(20)]
        [Display(Name = "Número de Estudante")]
        public required string StudentNumber { get; set; }
        
        public School? School { get; set; } // FK
    }
}

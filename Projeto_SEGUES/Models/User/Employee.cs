using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User
{
    public class Employee : User
    {
        [MaxLength(100)]
        [Display(Name = "Cargo")]
        public string? RoleDescription { get; set; }
        
        public School? School { get; set; } // FK
    }
}

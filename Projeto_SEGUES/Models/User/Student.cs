using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

public class Student : AppUser
{
    [Required]
    [MaxLength(20, ErrorMessage = "O número de estudante deve ter no máximo {1} caracteres.")]
    [Display(Name = "Número de Estudante")]
    public required string StudentNumber { get; set; }

    public School? School { get; set; } // FK
}
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

public class Employee : AppUser
{
    [MaxLength(100, ErrorMessage = "Cargo deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cargo")]
    public string? RoleDescription { get; set; }

    public School? School { get; set; } // FK
}
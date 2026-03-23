using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

public class Role : IdentityRole
{
    [Required]
    [MaxLength(100, ErrorMessage = "O nome de exibição deve ter no máximo {1} caracteres.")]
    [Display(Name = "Nome de Exibição")]
    public required string DisplayName { get; set; }
}
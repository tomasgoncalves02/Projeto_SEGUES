using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

public class Role : IdentityRole
{
    [Required]
    [MaxLength(100)]
    [Display(Name = "Nome de Exibição")]
    public required string DisplayName { get; set; }
}
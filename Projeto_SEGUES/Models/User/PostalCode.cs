using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

public class PostalCode
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(9, ErrorMessage = "O código postal deve ter no máximo {1} caracteres.")]
    [Display(Name = "Código Postal")]
    public required string Code { get; set; } // Ex: "2900-000"

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
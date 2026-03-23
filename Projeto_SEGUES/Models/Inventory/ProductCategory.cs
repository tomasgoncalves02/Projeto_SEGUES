using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Inventory;

public class ProductCategory
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo {1} caracteres.")]
    [Display(Name = "Nome")]
    public required string Name { get; set; }

    [Required]
    [MaxLength(250, ErrorMessage = "Descrição deve ter no máximo {1} caracteres.")]
    [Display(Name = "Descrição")]
    public required string Description { get; set; }
}
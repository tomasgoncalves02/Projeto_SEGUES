using Projeto_SEGUES.Models.Order;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Inventory;

public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Nome")]
    public required string Name { get; set; }

    [Required]
    [MaxLength(250)]
    [Display(Name = "Descrição")]
    public required string Description { get; set; }

    [Required]
    [Display(Name = "Categoria")]
    public required ProductCategory Category { get; set; } // FK

    [Required]
    [Range(0, double.MaxValue)]
    [Display(Name = "Preço")]
    public required decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    [Display(Name = "Stock")]
    public required int Stock { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    [Display(Name = "Stock mínimo")]
    public required int MinimumStock { get; set; }

    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;

    public ICollection<OrderLine> ProductPurchases { get; set; } = new List<OrderLine>();
}
using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Models.Bar;

public class BarOrder
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int ProductId { get; set; }
    public virtual Product? Product { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Range(0, double.MaxValue)]
    public decimal PriceAtTime { get; set; } // Congela o preço no momento da compra

    public bool IsConsumed { get; set; } = false;

    [MaxLength(10)]
    public string RedemptionCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
    // No teu BarOrder.cs
    public int Status { get; set; } = 0;
    // 0: Pendente, 1: Em preparação, 2: Entrega Pendente, 3: Entregue
}
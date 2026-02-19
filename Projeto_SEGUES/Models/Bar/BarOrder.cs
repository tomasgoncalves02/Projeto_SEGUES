using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_SEGUES.Models.Bar;

public class BarOrder
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual AppUser? User { get; set; }

    [Required]
    public int ProductId { get; set; }
    public virtual Product? Product { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public TimeSpan OrderPickUp { get; set; }

    public DateOnly Expired { get; set; }

    public DateOnly CreationTime { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    [Range(0, double.MaxValue)]
    public decimal PriceAtTime { get; set; } // Congela o preço no momento da compra

    public bool IsConsumed { get; set; } = false;

    [MaxLength(10)]
    public string RedemptionCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
    // No teu BarOrder.cs
    public int Status { get; set; } = 0;
    // 0: Pendente, 1: Em preparação, 2: Entrega Pendente, 3: Entregue
}
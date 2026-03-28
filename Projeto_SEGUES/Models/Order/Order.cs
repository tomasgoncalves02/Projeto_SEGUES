using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Order;

/// <summary>
/// Entity representing a commercial transaction for products within the SEGUES platform.
/// </summary>
/// <remarks>
/// This model orchestrates the order lifecycle, from a temporary 'Cart' to final delivery. 
/// It tracks temporal data (<see cref="DeliveryTime"/> and <see cref="PickupTime"/>) 
/// and handles secure redemption via a generated <see cref="RedemptionCode"/>.
/// </remarks>
public class Order
{
    /// <summary>Unique identifier for the order record.</summary>
    public int Id { get; set; }

    /// <summary>The final calculated monetary value of the order, including applied discounts.</summary>
    [Range(0, double.MaxValue)]
    [Display(Name = "Valor Total")]
    [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
    public decimal TotalValue { get; set; }

    /// <summary>The exact timestamp when the order was submitted by the user.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data do Pedido")]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    /// <summary>The estimated or scheduled time for the items to be prepared by staff.</summary>
    [DataType(DataType.Duration)]
    [DisplayFormat(DataFormatString = @"{0:hh\:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Entrega")]
    public TimeSpan? DeliveryTime { get; set; }

    /// <summary>The actual time when the user collected the products at the pickup point.</summary>
    [DataType(DataType.Duration)]
    [DisplayFormat(DataFormatString = @"{0:hh\:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Recolha")]
    public TimeSpan? PickupTime { get; set; }

    /// <summary>Navigation property to the user who placed the order.</summary>
    [Required]
    public required AppUser AppUser { get; set; } // FK

    /// <summary>Reference to an applied discount rule, if any was active during checkout.</summary>
    public Discount? Discount { get; set; }

    /// <summary>
    /// A unique, 8-character alphanumeric string used by staff to verify and redeem the order.
    /// Default: A truncated GUID in uppercase.
    /// </summary>
    [MaxLength(8)]
    [Display(Name = "Código")]
    public string RedemptionCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

    /// <summary>Current state of the order within the fulfillment pipeline.</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Cart;

    /// <summary>Detailed list of individual products and quantities associated with this order.</summary>
    public ICollection<OrderLine> ProductPurchases { get; set; } = new List<OrderLine>();
}
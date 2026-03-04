using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Models.Order
{
    public class Order
    {
        public int Id { get; set; }
        
        [Range(0, double.MaxValue)]
        [Display(Name = "Valor Total")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal TotalValue { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data do Pedido")]
        public DateTime OrderDate { get; set; } = DateTime.Now;
        
        [DataType(DataType.Duration)]
        [DisplayFormat(DataFormatString = "{0:c}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Entrega")]
        public TimeSpan? DeliveryTime { get; set; }
        
        [DataType(DataType.Duration)]
        [DisplayFormat(DataFormatString = "{0:c}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Recolha")]
        public TimeSpan? PickupTime { get; set; }
        
        [Required]
        public required AppUser AppUser { get; set; } // FK
        
        public Discount? Discount { get; set; }
        
        [MaxLength(8)]
        public string RedemptionCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        
        public OrderStatus Status { get; set; } = OrderStatus.Cart;

        public ICollection<OrderLine> ProductPurchases { get; set; } = new List<OrderLine>();
    }
}

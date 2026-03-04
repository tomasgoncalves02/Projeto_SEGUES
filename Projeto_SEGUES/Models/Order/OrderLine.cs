using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Models.Order
{
    public class OrderLine
    {
        [Required]
        public required int ProductId { get; set; }
        
        [Required]
        public required Product Product { get; set; } // FK
        
        [Required]
        public required int OrderId { get; set; }
        
        [Required]
        public required Order Order { get; set; } // FK

        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "Quantidade")]
        public required int Quantity { get; set; } = 1;
        
        public Discount? Discount { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Valor")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public required decimal ProductValue { get; set; } // Value at the time of purchase
    }
}

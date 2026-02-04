using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Models.Purchase
{
    public class ProductPurchase
    {
        [Required]
        public required int ProductId { get; set; }
        
        [Required]
        public required Product Product { get; set; } // FK
        
        [Required]
        public required int PurchaseId { get; set; }
        
        [Required]
        public required Purchase Purchase { get; set; } // FK

        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "Quantidade")]
        public required int Quantity { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Valor")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public required decimal ProductAddedValue { get; set; }
    }
}

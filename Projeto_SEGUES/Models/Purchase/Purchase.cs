using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Purchase
{
    public class Purchase
    {
        public int Id { get; set; }
        
        [Range(0, double.MaxValue)]
        [Display(Name = "Valor Total")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal TotalValue { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data da Transação")]
        public DateTime TransactionDate { get; set; }
        
        [Required]
        public required User.User User { get; set; } // FK

        public ICollection<ProductPurchase> ProductPurchases { get; set; } = new List<ProductPurchase>();
    }
}

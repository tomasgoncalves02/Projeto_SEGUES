using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Purchase;

namespace Projeto_SEGUES.Models.Inventory
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        [Display(Name = "Nome")]
        public required string Name { get; set; }
        
        //[Required]
        [MaxLength(250)]
        [Display(Name = "Descrição")]
        public required string Description { get; set; }
        
        //[Required]
        /*[MaxLength(100)]
        public required string ImageUrl { get; set; }*/
        
        //[Required]
        public ProductCategory? Category { get; set; } // FK
        
        [Range(0, double.MaxValue)]
        [Display(Name = "Preço")]
        public decimal Price { get; set; }
        
        [Range(0, int.MaxValue)]
        [Display(Name = "Stock")]
        public int Stock { get; set; }
        
        [Display(Name = "Ativo")]
        public bool IsActive { get; set; } = true;

        public ICollection<ProductPurchase> ProductPurchases { get; set; } = new List<ProductPurchase>();
    }
}

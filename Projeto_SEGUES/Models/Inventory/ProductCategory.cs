using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Inventory
{
    public class ProductCategory
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        [Display(Name = "Nome")]
        public required string Name { get; set; }
        
        [MaxLength(250)]
        [Display(Name = "Descrição")]
        public string? Description { get; set; }
    }
}

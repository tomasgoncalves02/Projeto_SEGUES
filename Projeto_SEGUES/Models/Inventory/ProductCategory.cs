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

        [Required]
        [MaxLength(250)]
        [Display(Name = "Descrição")]
        public required string Description { get; set; }
    }
}

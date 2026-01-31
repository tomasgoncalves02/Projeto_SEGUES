using System.ComponentModel.DataAnnotations.Schema;
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Models
{
    public class Discount
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; } // O valor do desconto
        public DiscountType DiscountType { get; set; } // Enum (Percentage ou Fixed)

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool Active { get; set; }

        // Relação com Product (fk: productId)
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}

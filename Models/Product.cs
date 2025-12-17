namespace Projeto_SEGUES.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string ImageCode { get; set; }

        public int CategoryId { get; set; }
        public ProductCategory Category { get; set; }

        public ICollection<ProductPurchase> ProductPurchases { get; set; }
    }
}

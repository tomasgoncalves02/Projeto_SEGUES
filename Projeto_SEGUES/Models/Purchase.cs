namespace Projeto_SEGUES.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime TransactionDate { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public ICollection<ProductPurchase> ProductPurchases { get; set; }
    }
}

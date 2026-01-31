using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_SEGUES.Models
{
    public class TicketPurchase
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Value { get; set; }

        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}

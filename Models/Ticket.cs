using System.ComponentModel.DataAnnotations.Schema;
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime PurchaseDate { get; set; }
        public TicketState State { get; set; }
        public DateTime? UsedDate { get; set; }

        public string OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public User Owner { get; set; }

        // Exemplo de relação com TicketPurchase (Assumindo 1:N ou 1:1)
        public int TicketPurchaseId { get; set; }
        public TicketPurchase TicketPurchase { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_SEGUES.Models
{
    public class TicketTransfer
    {
        public int Id { get; set; }
        public DateTime TransferDate { get; set; }

        public int TicketId { get; set; }
        [ForeignKey("TicketId")]
        public Ticket Ticket { get; set; }

        public string SenderId { get; set; }
        [ForeignKey("SenderId")]
        public User Sender { get; set; }

        public string ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }
    }
}

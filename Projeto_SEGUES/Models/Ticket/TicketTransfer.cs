using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Models.Ticket
{
    public class TicketTransfer
    {
        public int Id { get; set; }
        
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Transferência")]
        public DateTime TransferDate { get; set; }
        
        [Required]
        public required Ticket Ticket { get; set; } // FK
        
        [Required]
        public required AppUser Sender { get; set; } // FK
        
        [Required]
        public required AppUser Receiver { get; set; } // FK
    }
}

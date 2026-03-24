using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Ticket;

public class TicketTransfer
{
    public int Id { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Transferência")]
    public DateTime TransferDate { get; set; } = DateTime.Now;

    [Required]
    public required Ticket Ticket { get; set; } // FK

    [Required]
    [Display(Name = "Remetente")]
    public required AppUser Sender { get; set; } // FK

    [Required]
    [Display(Name = "Destinatário")]
    public required AppUser Receiver { get; set; } // FK
}
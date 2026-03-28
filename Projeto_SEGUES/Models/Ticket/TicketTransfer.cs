using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Ticket;

/// <summary>
/// Entity representing the movement of a digital ticket from one user to another.
/// </summary>
/// <remarks>
/// This model acts as a secondary ledger to the <see cref="Ticket"/> entity, 
/// providing a complete "Chain of Custody" for digital assets. It ensures 
/// accountability by recording both the <see cref="Sender"/> and <see cref="Receiver"/>.
/// </remarks>
public class TicketTransfer
{
    /// <summary>Unique identifier for the transfer event.</summary>
    public int Id { get; set; }

    /// <summary>The exact timestamp when the digital asset changed ownership.</summary>
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Transferência")]
    public DateTime TransferDate { get; set; } = DateTime.Now;

    /// <summary>Navigation property to the specific ticket being moved.</summary>
    [Required]
    public required Ticket Ticket { get; set; } // FK

    /// <summary>The user who initiated the transfer and surrendered ownership.</summary>
    [Required]
    [Display(Name = "Remetente")]
    public required AppUser Sender { get; set; } // FK

    /// <summary>The user who received the ticket and became the new owner.</summary>
    [Required]
    [Display(Name = "Destinatário")]
    public required AppUser Receiver { get; set; } // FK
}
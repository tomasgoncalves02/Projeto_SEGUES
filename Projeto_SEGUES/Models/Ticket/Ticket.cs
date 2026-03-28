using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Ticket;

/// <summary>
/// Entity representing a digital voucher for meals or campus services.
/// </summary>
/// <remarks>
/// This model manages the full lifecycle of a ticket, including ownership tracking, 
/// peer-to-peer transfers, and the secure validation process performed by staff.
/// </remarks>
public class Ticket
{
    /// <summary>Unique identifier for the digital ticket.</summary>
    public int Id { get; set; }

    /// <summary>The deadline after which the ticket is no longer valid for use.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Validade")]
    public DateTime ExpirationDate { get; set; }

    /// <summary>The current availability status (e.g., Available, Used, Expired).</summary>
    [Display(Name = "Estado")]
    public TicketState State { get; set; } = TicketState.Available;

    /// <summary>Flag indicating if the ticket has been redeemed at a collection point.</summary>
    [Display(Name = "Usado")]
    public bool IsUsed { get; set; } = false;

    /// <summary>The exact timestamp when the ticket was scanned and redeemed.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Uso")]
    public DateTime? UsedDate { get; set; }

    /// <summary>Navigation property to the current owner of the ticket.</summary>
    [Required]
    public required AppUser Owner { get; set; } // FK

    /// <summary>
    /// A unique 8-character code generated for scanning and verification.
    /// Default: A truncated GUID in uppercase.
    /// </summary>
    [MaxLength(8)]
    [Display(Name = "Código de Validação")]
    public string ValidationCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

    /// <summary>
    /// Reference to the Staff/Employee who performed the ticket validation.
    /// Remains null until the ticket is marked as Used.
    /// </summary>
    public AppUser? ValidatedBy { get; set; } // FK 

    /// <summary>Reference to the original purchase transaction that generated this ticket.</summary>
    public required TicketPurchase TicketPurchase { get; set; } // FK

    /// <summary>History of all transfers this ticket has undergone between users.</summary>
    public ICollection<TicketTransfer> Transfers { get; set; } = new List<TicketTransfer>();
}
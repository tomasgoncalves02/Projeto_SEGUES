using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Ticket;

/// <summary>
/// Entity representing the purchase event of one or more digital tickets.
/// </summary>
/// <remarks>
/// This model records the transaction details (quantity and total cost) and serves 
/// as the parent container for the generated <see cref="Ticket"/> collection.
/// </remarks>
public class TicketPurchase
{
    /// <summary>Unique identifier for the purchase record.</summary>
    public int Id { get; set; }

    /// <summary>Total number of tickets acquired in this specific transaction.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser pelo menos 1.")]
    [Display(Name = "Quantidade")]
    public int Quantity { get; set; }

    /// <summary>The exact date and time the purchase was finalized.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data da Transação")]
    public DateTime TransactionDate { get; set; }

    /// <summary>The total monetary value paid for this batch of tickets.</summary>
    [Range(0, double.MaxValue)]
    [Display(Name = "Valor")]
    [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
    public decimal Value { get; set; }

    /// <summary>Navigation property to the user who performed the purchase.</summary>
    [Required]
    public required AppUser AppUser { get; set; } // FK

    /// <summary>The specific digital ticket instances created by this purchase.</summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
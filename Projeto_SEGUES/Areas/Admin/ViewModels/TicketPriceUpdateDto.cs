namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// Data Transfer Object (DTO) used for updating existing ticket price records.
/// </summary>
/// <remarks>
/// This lightweight model is optimized for AJAX or Batch update operations 
/// within the Admin Dashboard, carrying only the minimum necessary payload 
/// (<see cref="Id"/> and <see cref="Price"/>) to reduce network overhead.
/// </remarks>
public class TicketPriceUpdateDto
{
    /// <summary>
    /// Constructor to initialize the DTO with the new price and the identifier of the record to update.
    /// </summary>
    /// <param name="price">The new price value to be assigned to the ticket price rule.</param>
    /// <param name="id">The unique identifier of the <c>TicketPrice</c> record that is being updated.</param>
    public TicketPriceUpdateDto(decimal price, int id)
    {
        Price = price;
        Id = id;
    }

    /// <summary>Unique identifier of the <c>TicketPrice</c> record to be modified.</summary>
    public int Id { get; }

    /// <summary>The new monetary value to be assigned to the ticket price rule.</summary>
    /// <remarks>
    /// Validation for this field is typically handled at the Service or Controller 
    /// level using the same constraints as the base <see cref="Models.Ticket.TicketPrice"/> entity.
    /// </remarks>
    public decimal Price { get; }
}
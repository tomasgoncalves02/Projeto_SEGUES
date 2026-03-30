using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Ticket.ViewModels;

/// <summary>
/// ViewModel used for the ticket validation interface at the point of service.
/// </summary>
/// <remarks>
/// This model handles the input of a specific ticket code and provides 
/// a historical list of recently processed tickets for immediate operator feedback.
/// </remarks>
public class ValidateTicketViewModel
{
    /// <summary>
    /// The unique 8-character alphanumeric code of the ticket to be validated.
    /// </summary>
    /// <value>A required string that must be exactly 8 characters long.</value>
    [Required(ErrorMessage = "Introduza o código da senha.")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "O código deve ter exatamente 8 caracteres.")]
    [Display(Name = "Código da Senha")]
    public string? Code { get; set; }

    /// <summary>
    /// A list of the most recently processed tickets used to provide visual confirmation to the operator.
    /// </summary>
    /// <remarks>
    /// This list helps prevent double-processing and allows for quick verification of the last successful actions.
    /// </remarks>
    public List<Models.Ticket.Ticket> RecentTickets { get; set; } = new();
}
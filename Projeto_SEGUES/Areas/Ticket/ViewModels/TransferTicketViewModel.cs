using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Projeto_SEGUES.Areas.Ticket.ViewModels;

/// <summary>
/// ViewModel used for the ticket transfer process between users.
/// </summary>
/// <remarks>
/// This model handles the selection of one or more tickets from the user's current 
/// inventory and validates the existence and format of the recipient's email address.
/// </remarks>
public class TransferTicketViewModel
{
    /// <summary>
    /// The email address of the user who will receive the transferred tickets.
    /// </summary>
    [Required(ErrorMessage = "Insira o email do destinatário.")]
    [EmailAddress(ErrorMessage = "Insira um email válido.")]
    [Display(Name = "Email do Destinatário")]
    public string? RecipientEmail { get; set; }

    /// <summary>
    /// A list of unique identifiers (GUIDs or IDs) for the tickets selected by the user for transfer.
    /// </summary>
    [Required(ErrorMessage = "Selecione pelo menos uma senha.")]
    public List<string> SelectedTickets { get; set; } = new();

    /// <summary>
    /// The collection of tickets currently owned by the user that are eligible for transfer.
    /// </summary>
    /// <remarks>
    /// Marked with [ValidateNever] to prevent the model binder from validating 
    /// the entire ticket entity tree during the POST request.
    /// </remarks>
    [ValidateNever]
    public List<Models.Ticket.Ticket> AvailableTickets { get; set; } = new();
}
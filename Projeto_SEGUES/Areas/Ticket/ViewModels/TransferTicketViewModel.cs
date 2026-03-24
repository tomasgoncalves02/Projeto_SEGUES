using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Projeto_SEGUES.Areas.Ticket.ViewModels;

public class TransferTicketViewModel
{
    [Required(ErrorMessage = "Insira o email do destinatário.")]
    [EmailAddress(ErrorMessage = "Insira um email válido.")]
    [Display(Name = "Email do Destinatário")]
    public string? RecipientEmail { get; set; } = null;
    
    [Required(ErrorMessage = "Selecione pelo menos uma senha.")]
    public List<string> SelectedTickets { get; set; } = new();
    
    [ValidateNever]
    public List<Models.Ticket.Ticket> AvailableTickets { get; set; } = new();
}
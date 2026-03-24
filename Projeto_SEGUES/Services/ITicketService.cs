using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public interface ITicketService
{
    // Active Tickets
    Task<List<Ticket>> GetActiveTicketsAsync(string userId);
    
    // Admin Logs
    Task<List<Ticket>> GetAllTicketsAsync();
    
    // Order Tickets
    Task<decimal> GetCurrentPriceForUserAsync(AppUser user);
    Task<List<Ticket>> GetUserTicketsAsync(string userId);
    Task<ServiceResult> BuyTicketsAsync(string userId, int quantity);
    
    // Report Tickets
    Task<List<Ticket>> QueryHistoryAsync(string userId, ReportTicketSearchViewModel model);
    
    // Transfer Tickets
    Task<ServiceResult<string>> CheckTransferEligibilityAsync(string senderId, string recipientEmail);
    Task<ServiceResult> TransferTicketsAsync(string senderId, string recipientEmail, List<string> selectedTickets);
    
    // Validate Tickets
    Task<List<Ticket>> GetRecentUsedTicketsAsync(int take = 10);
    Task<ServiceResult> ValidateTicketAsync(string code, AppUser validator);
}
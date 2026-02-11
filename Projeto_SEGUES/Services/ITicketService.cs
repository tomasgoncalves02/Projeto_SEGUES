using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public interface ITicketService
{    
    Task ExpireUserTicketsAsync(string userId);
    Task ExpireTicketsGlobalAsync();
    Task<List<Ticket>> GetUserTicketsAsync(string userId);
    Task<List<Ticket>> GetActiveTicketsAsync(string userId);
    Task<List<Ticket>> GetRecentUsedTicketsAsync(int take = 10);
    Task<decimal> GetCurrentPriceForUserAsync(AppUser user);
    Task<ServiceResult> BuyTicketsAsync(string userId, int quantity);
    Task<ServiceResult> ValidateTicketAsync(string code, AppUser validator);
    Task<List<Ticket>> QueryHistoryAsync(string userId, string searchString, TicketState? stateFilter, string flowFilter, DateTime? dateFilter);
    Task<List<Ticket>> GetAllTicketsAsync();
}
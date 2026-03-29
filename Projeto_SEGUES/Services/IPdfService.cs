using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Ticket;

namespace Projeto_SEGUES.Services;

public interface IPdfService
{
    byte[] GenerateAdminOrderHistoryPdfAsync(List<Order> orders, string logoPath);
    byte[] GenerateAdminTicketHistoryPdfAsync(List<Ticket> tickets, string logoPath);
}
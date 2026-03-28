using Projeto_SEGUES.Models.Order;

namespace Projeto_SEGUES.Services;

public interface IPdfService
{
    Task<byte[]> GenerateAdminOrderHistoryPdfAsync(List<Order> orders, string logoPath);
}
using Projeto_SEGUES.Models.Bar;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Services;

public interface IBarService
{
    Task<decimal> GetBalanceAsync(string userId);
    Task<List<Product>> GetAvailableProductsAsync();
    Task<List<BarOrder>> GetOrderHistoryAsync(string userId);
    Task<(bool Succeeded, string Message)> PlaceOrderAsync(string userId, int productId);
}
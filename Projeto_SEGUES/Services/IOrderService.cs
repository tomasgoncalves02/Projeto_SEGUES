using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public interface IOrderService
{
    Task<Order?> GetCartAsync(string userId, bool createIfNotFound = true);
    decimal ApplyDiscount(decimal price, Discount? discount);
    OrderTotalViewModel GetOrderTotal(Order cart);
    Task<ServiceResult> AddToCartAsync(string userId, int productId, int quantity);
    Task<ServiceResult> RemoveFromCartAsync(string userId, int productId);
    Task<ServiceResult> SubmitOrderAsync(AppUser user, bool receiveNow, string? pickupTime);
    Task<ServiceResult> CancelOrderAsync(int id);
    Task<List<Order>> GetActiveOrdersAsync(string userId);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<List<Order>> GetOrderHistoryAsync(string userId);
    Task<List<Order>> GetUndeliveredOrdersAsync();
    Task<List<Order>> GetAdminOrderHistoryAsync();
    Task<ServiceResult> UpdateOrderStatusAsync(int id, int newStatusId, AppUser staffMember);
    Task<ServiceResult> ValidateOrderCodeAsync(int id, string enteredCode, AppUser staffMember);
}
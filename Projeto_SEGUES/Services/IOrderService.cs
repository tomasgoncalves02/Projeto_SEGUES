using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public interface IOrderService
{
    // Create Order
    Task<Order?> GetCartAsync(string userId, bool createIfNotFound = true);
    decimal ApplyDiscount(decimal price, Discount? discount);
    OrderTotalViewModel GetOrderTotal(Order cart);
    Task<ServiceResult<OrderTotalViewModel>> AddToCartAsync(string userId, int productId, int quantity);
    Task<ServiceResult<OrderTotalViewModel>> RemoveFromCartAsync(string userId, int productId);
    Task<ServiceResult> SubmitOrderAsync(AppUser user, bool receiveNow, string? pickupTime);
    // Active Orders
    Task<ServiceResult> CancelOrderAsync(int id);
    Task<List<Order>> GetActiveOrdersAsync(string userId);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<List<Order>> GetUndeliveredOrdersAsync();
    // Validate Order
    Task<ServiceResult> UpdateOrderStatusAsync(int id, int newStatusId, AppUser staffMember);
    Task<ServiceResult> ValidateOrderCodeAsync(int id, string enteredCode, AppUser staffMember);
}
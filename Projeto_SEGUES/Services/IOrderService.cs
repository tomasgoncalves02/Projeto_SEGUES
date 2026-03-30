using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Interface for the Order and Cart Management Service.
/// Defines the required methods for handling the shopping cart, payment processing, 
/// order scheduling, and terminal validation by staff members.
/// </summary>
public interface IOrderService
{

    /// <summary>Retrieves the current active cart (Pending Order) for a specific user.</summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="createIfNotFound">If true, initializes a new empty order if none exists.</param>
    /// <returns>The Order entity acting as a cart, or null.</returns>
    Task<Order?> GetCartAsync(string userId, bool createIfNotFound = true);

    /// <summary>Calculates the final price of an item after applying a specific discount policy.</summary>
    /// <param name="price">The original product price.</param>
    /// <param name="discount">The discount entity to apply (Percentage or Fixed).</param>
    /// <returns>The discounted price value.</returns>
    decimal ApplyDiscount(decimal price, Discount? discount);

    /// <summary>Calculates the subtotal, discounts, and total amount for a given order.</summary>
    /// <param name="cart">The order entity containing items.</param>
    /// <returns>A ViewModel with the formatted financial breakdown.</returns>
    OrderTotalViewModel GetOrderTotal(Order cart);

    /// <summary>Adds a product to the user's cart or updates the quantity if it already exists.</summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="productId">The ID of the product to add.</param>
    /// <param name="quantity">The amount of the product.</param>
    /// <returns>A ServiceResult containing the updated order totals.</returns>
    Task<ServiceResult<OrderTotalViewModel>> AddToCartAsync(string userId, int productId, int quantity);

    /// <summary>Removes an entire product line from the user's shopping cart.</summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="productId">The ID of the product to remove.</param>
    /// <returns>A ServiceResult containing the updated order totals.</returns>
    Task<ServiceResult<OrderTotalViewModel>> RemoveFromCartAsync(string userId, int productId);

    /// <summary>Finalizes the order by processing payment (balance deduction) and generating a validation code.</summary>
    /// <param name="user">The user entity submitting the order.</param>
    /// <param name="receiveNow">If true, the order is marked for immediate pickup.</param>
    /// <param name="pickupTime">Optional scheduled time for later pickup.</param>
    /// <returns>A ServiceResult indicating success or payment/stock errors.</returns>
    Task<ServiceResult> SubmitOrderAsync(AppUser user, bool receiveNow, string? pickupTime);

    /// <summary>Cancels a paid order and refunds the amount to the user's balance.</summary>
    /// <param name="id">The Order ID.</param>
    /// <returns>A ServiceResult confirming the cancellation and refund.</returns>
    Task<ServiceResult> CancelOrderAsync(int id);

    /// <summary>Retrieves all active orders (Paid but not Delivered) for a specific user.</summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of active orders.</returns>
    Task<List<Order>> GetActiveOrdersAsync(string userId);

    /// <summary>Retrieves full details of a specific order by its ID.</summary>
    Task<Order?> GetOrderByIdAsync(int id);

    /// <summary>Retrieves all orders pending delivery for the staff administrative dashboard.</summary>
    /// <returns>A list of orders waiting for pickup.</returns>
    Task<List<Order>> GetUndeliveredOrdersAsync();

    /// <summary>Manually updates an order's status and logs the staff member responsible.</summary>
    /// <param name="id">The Order ID.</param>
    /// <param name="newStatusId">The target Status ID (e.g., Delivered, Cancelled).</param>
    /// <param name="staffMember">The employee performing the action.</param>
    /// <returns>A ServiceResult reflecting the change.</returns>
    Task<ServiceResult> UpdateOrderStatusAsync(int id, int newStatusId, AppUser staffMember);

    /// <summary>Validates an order's unique alphanumeric code (scanned or typed) at the pickup point.</summary>
    /// <param name="id">The Order ID.</param>
    /// <param name="enteredCode">The 8-character code presented by the user.</param>
    /// <param name="staffMember">The employee validating the code.</param>
    /// <returns>A ServiceResult confirming delivery or invalid code.</returns>
    Task<ServiceResult> ValidateOrderCodeAsync(int id, string enteredCode, AppUser staffMember);

}
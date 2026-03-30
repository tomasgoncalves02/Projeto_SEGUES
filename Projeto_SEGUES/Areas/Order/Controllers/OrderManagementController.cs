using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order.Controllers;

/// <summary>
/// Controller responsible for the operational management of orders by staff and administrators.
/// </summary>
/// <remarks>
/// This controller allows staff members to monitor pending orders, update production statuses, 
/// and validate pickup codes to complete the delivery cycle to the end user.
/// </remarks>
[Authorize(Roles = "Admin, Employee")]
[Area("Order")]
public class OrderManagementController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Initializes a new instance of the controller with order, identity, logging, and localization services.
    /// </summary>
    public OrderManagementController(
        IOrderService orderService,
        UserManager<AppUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    /// <summary>
    /// Displays the main management interface for undelivered orders.
    /// </summary>
    /// <returns>The Index View with the list of pending orders. Redirects to error on failure.</returns>
    public async Task<IActionResult> Index()
    {
        return View(await _orderService.GetUndeliveredOrdersAsync());
    }

    /// <summary>
    /// Gets only the orders table for partial UI updates via HTMX/AJAX.
    /// </summary>
    /// <returns>A PartialView containing the updated undelivered orders table.</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrdersTable()
    {
        return PartialView("_ManageOrdersTablePartial", await _orderService.GetUndeliveredOrdersAsync());
    }

    /// <summary>
    /// Retrieves specific order details for display in a side panel (Side Card).
    /// </summary>
    /// <param name="id">Unique order identifier.</param>
    /// <returns>A PartialView with order details or NotFound if the order does not exist.</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrderDetailsSide(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Challenge();
        }
        bool isStaff = User.IsInRole("Admin") || User.IsInRole("Employee");
        
        OrderManagementSideViewModel vm = new OrderManagementSideViewModel
        {
            Id = order.Id,
            FormattedTotalValue = order.TotalValue.ToString("C"),
            TotalQuantity = _orderService.GetOrderTotal(order).TotalQuantity,
            BuyerName = $"{order.AppUser.FirstName} {order.AppUser.LastName}",

            // Status
            CurrentStatusId = (int)order.Status,
            StatusDisplayName = order.Status.ToDisplayName(),
            StatusBadgeClass = order.Status.ToString().ToBadgeClass(),
        
            PrevStatusId = (int)order.Status - 1,
            CanGoBack = order.Status is > OrderStatus.Pending and < OrderStatus.Delivered,
        
            NextStatusId = (int)order.Status + 1,
            CanGoForward = order.Status is >= OrderStatus.Pending and < OrderStatus.Delivered,

            // Map products
            Items = order.ProductPurchases.Select(p => new OrderProductDto
            {
                Id = p.ProductId,
                Name = p.Product.Name,
                Price = p.ProductValue,
                Quantity = p.Quantity,
                CategoryName = p.Product.Category.Name,
                ModalInfo = new 
                {
                    name = p.Product.Name,
                    description = p.Product.Description,
                    price = p.ProductValue.ToString("C"),
                    categoryName = p.Product.Category.Name,
                    categoryDescription = p.Product.Category.Description,
                    stock = isStaff ? (int?)p.Product.Stock : null,
                    minStock = isStaff ? (int?)p.Product.MinimumStock : null
                }
            }).ToList()
        };
        
        return PartialView("_ManageOrderDetailsSideCardPartial", vm);
    }

    /// <summary>
    /// Updates the status of an order (e.g., In Preparation, Ready).
    /// </summary>
    /// <param name="id">ID of the order to update.</param>
    /// <param name="newStatus">Integer representation of the new status (OrderStatus Enum).</param>
    /// <returns>JSON success message or error response.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, int newStatus)
    {
        var staffMember = await _userManager.GetUserAsync(User);
        if (staffMember == null || (!User.IsInRole("Admin") && !User.IsInRole("Employee")))
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Challenge();
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, newStatus, staffMember);

        if (!result.Success)
            return BadRequest(new { failMessage = result.Message });

        return Ok(new { successMessage = result.Message });
    }

    /// <summary>
    /// Validates the redemption code entered by the staff to confirm order delivery.
    /// </summary>
    /// <param name="id">ID of the order to validate.</param>
    /// <param name="enteredCode">Alphanumeric code provided by the customer.</param>
    /// <returns>JSON result of the operation.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateOrderCode(int id, string enteredCode)
    {
        var staffMember = await _userManager.GetUserAsync(User);
        if (staffMember == null || (!User.IsInRole("Admin") && !User.IsInRole("Employee")))
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Challenge();
        }

        var result = await _orderService.ValidateOrderCodeAsync(id, enteredCode, staffMember);

        if (!result.Success)
            return BadRequest(new { failMessage = result.Message });

        return Ok(new { successMessage = result.Message });
    }
}
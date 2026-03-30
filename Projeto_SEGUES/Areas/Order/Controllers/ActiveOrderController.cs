using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order.Controllers;

/// <summary>
/// Controller responsible for managing and viewing the authenticated user's active orders.
/// </summary>
/// <remarks>
/// This controller allows users to check the status of their ongoing orders, 
/// view specific details, and perform order cancellations when permitted by business rules.
/// </remarks>
[Area("Order")]
[Authorize]
public class ActiveOrderController : Controller
{
    private readonly IOrderService _orderService;

    /// <summary>
    /// Initializes a new instance of the controller with user, order, logging, and localization services.
    /// </summary>
    public ActiveOrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Displays the list of active orders (processing or ready) for the current user.
    /// </summary>
    /// <returns>The Index View with the active orders collection. Redirects to error on query failure.</returns>
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var orders = await _orderService.GetActiveOrdersAsync(userId);
        return View(orders);
    }

    /// <summary>
    /// Optimized endpoint for HTMX that returns only the active order cards for UI updates.
    /// </summary>
    /// <returns>A PartialView with updated cards or 500 status on failure.</returns>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedActiveOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) 
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Unauthorized();
        }
        
        var orders = await _orderService.GetActiveOrdersAsync(userId);
        return PartialView("_ActiveOrdersCardsPartial", orders);
    }

    /// <summary>
    /// Displays detailed information for a specific order with ownership validation.
    /// </summary>
    /// <param name="id">Unique order identifier.</param>
    /// <returns>Details View or redirect if the order is not found or doesn't belong to the user.</returns>
    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (order == null || !order.Status.IsActive() || order.AppUser.Id != userId)
        {
            TempData.SetSwalError("O pedido solicitado não foi encontrado ou não tem permissão para o ver.");
            return RedirectToAction(nameof(Index));
        }
        
        bool isStaff = User.IsInRole("Admin") || User.IsInRole("Employee");
        
        var model = new OrderDetailsViewModel
        {
            Order = order,
            TotalQuantity = _orderService.GetOrderTotal(order).TotalQuantity,
            
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
        
        return View(model);
    }

    /// <summary>
    /// Processes the cancellation request for an active order based on time and status rules.
    /// </summary>
    /// <param name="id">ID of the order to cancel.</param>
    /// <returns>Redirects to index with a success or error SweetAlert.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var result = await _orderService.CancelOrderAsync(id);

        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
        }
        else
        {
            TempData.SetSwalError(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }
}
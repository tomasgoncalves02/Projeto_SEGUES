using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

/// <summary>
/// Controller responsible for creating new orders and managing the shopping cart lifecycle.
/// </summary>
/// <remarks>
/// This controller coordinates the interaction between product inventory and the order service, 
/// allowing item addition/removal and the checkout process with balance and schedule validations.
/// </remarks>
[Area("Order")]
[Authorize]
public class CreateOrderController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Initializes the controller with inventory, order, identity, administration, logging, and localization services.
    /// </summary>
    public CreateOrderController(
        IInventoryService inventoryService,
        IOrderService orderService,
        UserManager<AppUser> userManager)
    {
        _inventoryService = inventoryService;
        _orderService = orderService;
        _userManager = userManager;
    }

    /// <summary>
    /// Displays the product selection page for a new order.
    /// </summary>
    /// <returns>A View with the available products. Redirects to a global error page if the query fails.</returns>
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var cart = await _orderService.GetCartAsync(userId);
        bool isStaff = User.IsInRole("Admin") || User.IsInRole("Employee");
        
        var rawProducts = await _inventoryService.GetAvailableProductsAsync();

        CreateOrderViewModel vm = new CreateOrderViewModel
        {
            Categories = await _inventoryService.GetAllCategoriesForDropdownAsync(),
            CartTotal = cart != null
                ? _orderService.GetOrderTotal(cart)
                : new OrderTotalViewModel { TotalQuantity = 0, TotalValue = 0m },
            Products = rawProducts.Select(p => new OrderProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                CategoryId = p.Category.Id,
                CategoryName = p.Category.Name,
                ModalInfo = new
                {
                    name = p.Name,
                    description = p.Description,
                    price = p.Price.ToString("C"),
                    categoryName = p.Category.Name,
                    categoryDescription = p.Category.Description,
                    stock = isStaff ? (int?) p.Stock : null,
                    minStock = isStaff ? (int?) p.MinimumStock : null
                }
            }).ToList()
        };
        
        return View(vm);
    }

    /// <summary>
    /// Adds a product to the user's cart via AJAX.
    /// </summary>
    /// <param name="id">Product unique identifier.</param>
    /// <param name="qty">Desired quantity.</param>
    /// <returns>A JSON object indicating success or failure (404/500).</returns>
    [HttpPost]
    public async Task<IActionResult> AddToCart(int id, int qty)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) 
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Challenge();
        }

        var result = await _orderService.AddToCartAsync(userId, id, qty);
        if (!result.Success) return NotFound(new { failMessage = result.Message });

        OrderTotalViewModel orderTotal = result.Data!;
        return Ok(new { successMessage = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
    }

    /// <summary>
    /// Removes a specific product from the cart via AJAX.
    /// </summary>
    /// <param name="id">Product unique identifier.</param>
    /// <returns>A JSON object with the updated cart state or 500 status on error.</returns>
    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) 
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Unauthorized();
        }
        
        var result = await _orderService.RemoveFromCartAsync(userId, id);
        if (!result.Success) return NotFound(new { failMessage = result.Message });

        OrderTotalViewModel orderTotal = result.Data!;
        return Ok(new { successMessage = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
    }

    /// <summary>
    /// Displays the checkout page with order summary and user balance.
    /// </summary>
    /// <returns>The Checkout View or a redirect if the cart/balance cannot be retrieved.</returns>
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        ViewBag.Balance = user.Balance;
        var cart = await _orderService.GetCartAsync(user.Id);

        if (cart == null) return RedirectToAction(nameof(Index));

        ViewBag.TotalQuantity = _orderService.GetOrderTotal(cart).TotalQuantity;
        return View(cart);
    }

    /// <summary>
    /// Processes the final order submission, validating stock, balance, and pickup schedules.
    /// </summary>
    /// <param name="receiveNow">Flag for immediate pickup.</param>
    /// <param name="pickupTime">Optional scheduled time for pickup.</param>
    /// <returns>Redirects to active orders on success, or back to checkout with a SweetAlert on failure.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOrder(bool receiveNow, string? pickupTime)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        
        var result = await _orderService.SubmitOrderAsync(user, receiveNow, pickupTime);

        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction("Index", "ActiveOrder", new { area = "Order" });
        }
        
        TempData.SetSwalError(result.Message);
        return RedirectToAction(nameof(Checkout));
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
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
    private readonly IAdminService _adminService;
    private readonly ILogger<CreateOrderController> _logger;
    private readonly IStringLocalizer<Errors> _localizer;

    /// <summary>
    /// Initializes the controller with inventory, order, identity, administration, logging, and localization services.
    /// </summary>
    public CreateOrderController(
        IInventoryService inventoryService,
        IOrderService orderService,
        UserManager<AppUser> userManager,
        IAdminService adminService,
        ILogger<CreateOrderController> logger,
        IStringLocalizer<Errors> localizer)
    {
        _inventoryService = inventoryService;
        _orderService = orderService;
        _userManager = userManager;
        _adminService = adminService;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Displays the product selection page for a new order.
    /// </summary>
    /// <returns>A View with the available products. Redirects to a global error page if the query fails.</returns>
    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var cart = await _orderService.GetCartAsync(userId);
            if (cart != null)
            {
                ViewBag.CartTotal = _orderService.GetOrderTotal(cart);
            }

            ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
            return View(await _inventoryService.GetAvailableProductsAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao carregar a loja (Index).");
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
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
        try
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var result = await _orderService.AddToCartAsync(userId, id, qty);
            if (!result.Success) return NotFound(new { failMessage = result.Message });

            OrderTotalViewModel orderTotal = (OrderTotalViewModel)result.Data!;
            return Ok(new { successMessage = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.DatabaseConnectionError, TableName.Order, AppOperation.Create, ex);
            return StatusCode(500, new { failMessage = "Erro interno ao processar carrinho." });
        }
    }

    /// <summary>
    /// Removes a specific product from the cart via AJAX.
    /// </summary>
    /// <param name="id">Product unique identifier.</param>
    /// <returns>A JSON object with the updated cart state or 500 status on error.</returns>
    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var result = await _orderService.RemoveFromCartAsync(userId, id);
            if (!result.Success) return NotFound(new { failMessage = result.Message });

            OrderTotalViewModel orderTotal = (OrderTotalViewModel)result.Data!;
            return Ok(new { successMessage = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.DatabaseQueryError, TableName.Order, AppOperation.Delete, ex);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Displays the checkout page with order summary and user balance.
    /// </summary>
    /// <returns>The Checkout View or a redirect if the cart/balance cannot be retrieved.</returns>
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ViewBag.Balance = user.Balance;
            var cart = await _orderService.GetCartAsync(user.Id);

            if (cart == null) return RedirectToAction(nameof(Index));

            ViewBag.TotalQuantity = _orderService.GetOrderTotal(cart).TotalQuantity;
            return View(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar Checkout.");
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
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

        try
        {
            var result = await _orderService.SubmitOrderAsync(user, receiveNow, pickupTime);

            if (result.Success)
            {
                TempData.SetSwalSuccess(result.Message);
                return RedirectToAction("Index", "ActiveOrder", new { area = "Order" });
            }

            // Erro de negócio (Saldo insuficiente, stock, bar fechado, etc.)
            TempData.SetSwalError(result.Message);
            return RedirectToAction(nameof(Checkout));
        }
        catch (Exception ex)
        {
            // Erro crítico técnico (Ex: Falha no SaveChanges ou SQL)
            _logger.LogAppError(AppErrors.OrderProcessingError, TableName.Order, AppOperation.Create, ex);

            var erroEnum = AppErrors.OrderProcessingError;
            var msg = $"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]";

            TempData.SetSwalError(msg);
            return RedirectToAction(nameof(Checkout));
        }
    }
}
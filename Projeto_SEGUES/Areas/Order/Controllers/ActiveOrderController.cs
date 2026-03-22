using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

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
    private readonly UserManager<AppUser> _userManager;
    private readonly IOrderService _orderService;
    private readonly ILogger<ActiveOrderController> _logger;
    private readonly IStringLocalizer<Errors> _localizer;

    /// <summary>
    /// Initializes a new instance of the controller with user, order, logging, and localization services.
    /// </summary>
    public ActiveOrderController(
        UserManager<AppUser> userManager,
        IOrderService orderService,
        ILogger<ActiveOrderController> logger,
        IStringLocalizer<Errors> localizer)
    {
        _userManager = userManager;
        _orderService = orderService;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Displays the list of active orders (processing or ready) for the current user.
    /// </summary>
    /// <returns>The Index View with the active orders collection. Redirects to error on query failure.</returns>
    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var orders = await _orderService.GetActiveOrdersAsync(userId);
            return View(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao carregar Index de Pedidos Ativos.");
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
    }

    /// <summary>
    /// Optimized endpoint for HTMX that returns only the active order cards for UI updates.
    /// </summary>
    /// <returns>A PartialView with updated cards or 500 status on failure.</returns>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedActiveOrders()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _orderService.GetActiveOrdersAsync(userId!);
            return PartialView("_ActiveOrdersCards", orders);
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.DatabaseQueryError, TableName.Order, AppOperation.Read, ex);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Displays detailed information for a specific order with ownership validation.
    /// </summary>
    /// <param name="id">Unique order identifier.</param>
    /// <returns>Details View or redirect if the order is not found or doesn't belong to the user.</returns>
    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            var currentUserId = _userManager.GetUserId(User);

            if (order == null || !order.Status.IsActive() || order.AppUser.Id != currentUserId)
            {
                TempData.SetSwalError("O pedido solicitado não foi encontrado ou não tem permissão para o ver.");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TotalQuantity = _orderService.GetOrderTotal(order).TotalQuantity;
            return View(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao carregar detalhes do pedido {id}.");
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
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
        try
        {
            var result = await _orderService.CancelOrderAsync(id);

            if (!result.Success)
            {
                TempData.SetSwalError(result.Message);
                return RedirectToAction(nameof(Index));
            }

            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.OrderCancelError, TableName.Order, AppOperation.Update, ex);

            var erroEnum = AppErrors.OrderCancelError;
            var msg = $"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]";

            TempData.SetSwalError(msg);
            return RedirectToAction(nameof(Index));
        }
    }
}
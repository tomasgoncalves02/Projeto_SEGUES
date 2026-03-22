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
    private readonly ILogger<OrderManagementController> _logger;
    private readonly IStringLocalizer<Errors> _localizer;

    /// <summary>
    /// Initializes a new instance of the controller with order, identity, logging, and localization services.
    /// </summary>
    public OrderManagementController(
        IOrderService orderService,
        UserManager<AppUser> userManager,
        ILogger<OrderManagementController> logger,
        IStringLocalizer<Errors> localizer)
    {
        _orderService = orderService;
        _userManager = userManager;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Displays the main management interface for undelivered orders.
    /// </summary>
    /// <returns>The Index View with the list of pending orders. Redirects to error on failure.</returns>
    public async Task<IActionResult> Index()
    {
        try
        {
            return View(await _orderService.GetUndeliveredOrdersAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao carregar gestão de pedidos.");
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
    }

    /// <summary>
    /// Gets only the orders table for partial UI updates via HTMX/AJAX.
    /// </summary>
    /// <returns>A PartialView containing the updated undelivered orders table.</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrdersTable()
    {
        try
        {
            return PartialView("_ManageOrdersTablePartial", await _orderService.GetUndeliveredOrdersAsync());
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Erro AJAX na tabela de gestão: {ex.Message}", TableName.Order, AppOperation.Read);

            var erroEnum = AppErrors.DatabaseQueryError;
            var msg = $"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]";

            return StatusCode(500, new { failMessage = msg });
        }
    }

    /// <summary>
    /// Retrieves specific order details for display in a side panel (Side Card).
    /// </summary>
    /// <param name="id">Unique order identifier.</param>
    /// <returns>A PartialView with order details or NotFound if the order does not exist.</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrderDetailsSide(int id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            ViewBag.TotalQuantity = _orderService.GetOrderTotal(order).TotalQuantity;
            return PartialView("_ManageOrderDetailsSideCardPartial", order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao carregar side card do pedido {id}.");
            return StatusCode(500);
        }
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
        try
        {
            var staffMember = await _userManager.GetUserAsync(User);
            if (staffMember == null) return Unauthorized();

            var result = await _orderService.UpdateOrderStatusAsync(id, newStatus, staffMember);

            if (!result.Success)
                return BadRequest(new { failMessage = result.Message });

            return Ok(new { successMessage = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Erro ao atualizar estado do pedido {id}: {ex.Message}", TableName.Order, AppOperation.Update);

            var erroEnum = AppErrors.DatabaseUpdateError;
            var msg = $"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]";

            return StatusCode(500, new { failMessage = msg });
        }
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
        try
        {
            var staffMember = await _userManager.GetUserAsync(User);
            if (staffMember == null) return Unauthorized();

            var result = await _orderService.ValidateOrderCodeAsync(id, enteredCode, staffMember);

            if (!result.Success)
                return BadRequest(new { failMessage = result.Message });

            return Ok(new { successMessage = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Erro ao validar código do pedido {id}: {ex.Message}", TableName.Order, AppOperation.Update);

            var erroEnum = AppErrors.DatabaseUpdateError;
            var msg = $"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]";

            return StatusCode(500, new { failMessage = msg });
        }
    }
}
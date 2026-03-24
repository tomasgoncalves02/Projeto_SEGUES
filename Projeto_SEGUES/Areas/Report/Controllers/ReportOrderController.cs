using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Report;

/// <summary>
/// Controller responsible for viewing order history and specific order details for the user.
/// </summary>
/// <remarks>
/// This controller provides tools for users to consult their past purchases 
/// and retrieve technical details about each transaction via an internal API.
/// </remarks>
[Area("Report")]
[Authorize]
public class ReportOrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<ReportOrderController> _logger;

    /// <summary>
    /// Initializes a new instance of the controller with order, identity, and logging services.
    /// </summary>
    public ReportOrderController(
        IOrderService orderService,
        UserManager<AppUser> userManager,
        ILogger<ReportOrderController> logger)
    {
        _orderService = orderService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Displays the full order history for the authenticated user.
    /// </summary>
    /// <returns>
    /// The Index View with the user's order list. 
    /// Redirects to a global error page if the database query fails.
    /// </returns>
    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var history = await _orderService.GetOrderHistoryAsync(userId);
            return View(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao carregar o histórico de pedidos.");

            // Usando a tua chave 'DatabaseQueryError' e o Enum 1001
            return RedirectToAction("Error", "Home", new
            {
                area = "",
                errorCode = (int)AppErrors.DatabaseQueryError
            });
        }
    }

    /// <summary>
    /// Provides specific order details in JSON format for client-side consumption.
    /// </summary>
    /// <param name="id">Unique order identifier.</param>
    /// <returns>
    /// A JSON object containing the redemption code and product list, or authorization/not found error.
    /// </returns>
    /// <remarks>
    /// Includes a security check to prevent regular users from viewing third-party order details.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);

            // Usando a tua chave 'NotFound' do Errors.resx
            if (order == null)
                return NotFound(new { failMessage = Errors.NotFound });

            var currentUserId = _userManager.GetUserId(User);

            // Verificação de segurança: se não for Admin, só vê o que lhe pertence
            if (!User.IsInRole("Admin") && order.AppUser.Id != currentUserId)
            {
                // Usando a tua chave 'Unauthorized' do Errors.resx
                return StatusCode(403, new { failMessage = Errors.Unauthorized });
            }

            return Ok(new
            {
                code = order.RedemptionCode,
                products = order.ProductPurchases.Select(p => new
                {
                    name = p.Product.Name,
                    quantity = p.Quantity,
                    price = p.ProductValue
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.InternalServerError,
                                TableName.Order,
                                AppOperation.Read, ex);

            // Usando a tua chave 'InternalServerError' e o Enum 1501
            var msg = $"{Errors.InternalServerError} [Erro: {(int)AppErrors.InternalServerError}]";

            return StatusCode(500, new { failMessage = msg });
        }
    }
}
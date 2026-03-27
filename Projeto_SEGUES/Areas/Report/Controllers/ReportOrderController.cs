using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Report.ViewModels;
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
    private readonly IReportService _reportService;
    private readonly IOrderService _orderService;

    /// <summary>
    /// Initializes a new instance of the controller with order, identity, and logging services.
    /// </summary>
    public ReportOrderController(IReportService reportService, IOrderService orderService)
    {
        _reportService = reportService;
        _orderService = orderService;
    }

    /// <summary>
    /// Displays the full order history for the authenticated user.
    /// </summary>
    /// <returns>
    /// The Index View with the user's order list. 
    /// Redirects to a global error page if the database query fails.
    /// </returns>
    public async Task<IActionResult> Index(ReportOrderSearchViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        model.Results = await _reportService.GetOrderHistoryAsync(userId, model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetFilteredOrders(ReportOrderSearchViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Challenge();
        }
        
        var results = await _reportService.GetOrderHistoryAsync(userId, model);
        return PartialView("_OrderHistoryRowsPartial", results);
    }

    /// <summary>
    /// Returns detailed information about a specific order in JSON format.
    /// </summary>
    /// <param name="id">The unique identifier of the order.</param>
    /// <returns>
    /// A JSON object containing the redemption code and product list, 
    /// or an error message if the order is not found or access is denied.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        var order = await _orderService.GetOrderByIdAsync(id);

        if (order == null || (!User.IsInRole("Admin") && order.AppUser.Id != userId))
        {
            return Json(new { failMessage = "O pedido solicitado não foi encontrado ou não tem permissão para o ver." });
        }

        return Json(new
        {
            code = order.RedemptionCode,
            products = order.ProductPurchases.Select(pp => new
            {
                name = pp.Product.Name,
                quantity = pp.Quantity,
                price = pp.ProductValue.ToString("C"),
                categoryName = pp.Product.Category.Name,
                categoryDescription = pp.Product.Category.Description
            }).ToList()
        });
    }
}
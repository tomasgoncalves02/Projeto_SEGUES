using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Report;

[Area("Report")]
public class ReportOrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;

    public ReportOrderController(IOrderService orderService, UserManager<AppUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }
        
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        return View(await _orderService.GetOrderHistoryAsync(userId));
    }
    
    // Endpoint for HTMX update of order details
    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int id) 
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        
        if (order?.AppUser.Id != _userManager.GetUserId(User))
            order = null;
        return Json(new
        {
            produtos = order?.ProductPurchases.Select(p => new { nome = p.Product.Name, quantidade = p.Quantity, preco = p.Product.Price }),
            codigo = order?.RedemptionCode
        });
    }
}
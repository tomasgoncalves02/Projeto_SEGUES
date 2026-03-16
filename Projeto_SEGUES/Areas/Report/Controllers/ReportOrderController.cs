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
    
    // API access point for JS showOrderDetails function
    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int id) 
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        
        // Security check: if the user is not admin, do not show order info if it does not belong to the user
        if (!User.IsInRole("Admin") && order.AppUser.Id != _userManager.GetUserId(User))
            return Unauthorized();
        
        return Json(new
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
}
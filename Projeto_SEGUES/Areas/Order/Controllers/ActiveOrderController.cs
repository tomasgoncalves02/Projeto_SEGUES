using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

[Area("Order")]
[Authorize]
public class ActiveOrderController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IOrderService _orderService;
    
    public ActiveOrderController (UserManager<AppUser> userManager, IOrderService orderService)
    {
        _userManager = userManager;
        _orderService = orderService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        return View(await _orderService.GetActiveOrdersAsync(userId!));
    }
    
    // Endpoint for HTMX update of active orders table
    [HttpGet]
    public async Task<IActionResult> GetUpdatedActiveOrders()
    {
        var userId = _userManager.GetUserId(User);
        return PartialView("_ActiveOrdersCards", await _orderService.GetActiveOrdersAsync(userId!));
    }
    
    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null || !order.Status.IsActive() || order.AppUser.Id != _userManager.GetUserId(User))
        {
            TempData.SetSwalError("Pedido não encontrado.");
            return RedirectToAction(nameof(Index));
        }
        ViewBag.TotalQuantity = _orderService.GetOrderTotal(order).TotalQuantity;
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> CancelOrder(int id)
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
}
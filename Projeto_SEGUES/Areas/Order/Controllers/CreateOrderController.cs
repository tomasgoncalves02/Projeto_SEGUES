using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

[Area("Order")]
[Authorize]
public class CreateOrderController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;

    public CreateOrderController(
        IInventoryService inventoryService,
        IOrderService orderService,
        UserManager<AppUser> userManager,
        IAdminService adminService
    )
    {
        _inventoryService = inventoryService;
        _orderService = orderService;
        _userManager = userManager;
        _adminService = adminService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var cart = await _orderService.GetCartAsync(userId);
        ViewBag.CartTotal = _orderService.GetOrderTotal(cart);
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
        return View(await _inventoryService.GetAvailableProductsAsync());
    }
    
    [HttpPost]
    public async Task<IActionResult> AddToCart(int id, int qty)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _orderService.AddToCartAsync(userId, id, qty);
        OrderTotalViewModel orderTotal = (OrderTotalViewModel) result.Data!;
        return Json(new { success = result.Success, message = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
    }
    
    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        var userId = _userManager.GetUserId(User);
        var result = await _orderService.RemoveFromCartAsync(userId!, id);
        OrderTotalViewModel orderTotal = (OrderTotalViewModel) result.Data!;
        return Json(new { success = result.Success, message = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
    }
    
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var user = await _userManager.GetUserAsync(User);
        ViewBag.Balance = user!.Balance;
        var cart = await _orderService.GetCartAsync(user.Id);
        ViewBag.TotalQuantity = _orderService.GetOrderTotal(cart).TotalQuantity;
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOrder(bool receiveNow, string? pickupTime)
    {        
        var user = await _userManager.GetUserAsync(User);
        
        var result = await _orderService.SubmitOrderAsync(user!, receiveNow, pickupTime);

        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction("Index", "ActiveOrder", new { area = "Order" });
        }
        TempData.SetSwalError(result.Message);
        return RedirectToAction(nameof(Checkout));
    }
}
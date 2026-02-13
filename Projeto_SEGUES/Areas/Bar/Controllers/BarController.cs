using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Services;
using Projeto_SEGUES.Extensions; // Para TempData.SetSwalSuccess
using System.Security.Claims;

namespace Projeto_SEGUES.Areas.Bar.Controllers;

[Authorize]
[Area("Bar")]
public class BarController : Controller
{
    private readonly IBarService _barService;

    public BarController(IBarService barService) => _barService = barService;

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        ViewBag.UserBalance = await _barService.GetBalanceAsync(userId);
        return View();
    }

    public async Task<IActionResult> CreateOrder()
    {
        var products = await _barService.GetAvailableProductsAsync();
        return View(products);
    }

    [HttpPost]
    public async Task<IActionResult> Purchase(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _barService.PlaceOrderAsync(userId, productId);

        if (result.Succeeded)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction(nameof(OrderHistory));
        }

        TempData.SetSwalError(result.Message);
        return RedirectToAction(nameof(CreateOrder));
    }

    public async Task<IActionResult> OrderHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var history = await _barService.GetOrderHistoryAsync(userId);
        return View(history);
    }
}
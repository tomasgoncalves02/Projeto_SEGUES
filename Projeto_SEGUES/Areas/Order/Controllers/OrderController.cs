using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

[Authorize]
[Area("Order")]
public class OrderController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;

    public OrderController(UserManager<AppUser> userManager, IAdminService adminService)
    {
        _userManager = userManager;
        _adminService = adminService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        ViewBag.UserBalance = user.Balance;      
        ViewBag.OpeningTime = (await _adminService.GetOpenBarTimeAsync()).ToString(@"hh\:mm");
        ViewBag.ClosingTime = (await _adminService.GetCloseBarTimesAsync()).ToString(@"hh\:mm");

        return View();
    }
}
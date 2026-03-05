using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminOrderManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;

    public AdminOrderManagementController(IAdminService adminService, IOrderService orderService, UserManager<AppUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
        _adminService = adminService;
    }
    
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        ViewBag.OpenBarTime = await _adminService.GetOpenBarTimeAsync();
        ViewBag.CloseBarTime = await _adminService.GetCloseBarTimesAsync();
        return View(await _orderService.GetAdminOrderHistoryAsync(userId!));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOpenAndCloseTime(TimeSpan openTime, TimeSpan closeTime)
    {
        if (openTime == closeTime)
        {
            TempData.SetSwalError("A hora de abertura e de fecho não podem ser iguais.");
            return RedirectToAction(nameof(Index));
        }

        if (closeTime < openTime)
        {
            TempData.SetSwalError("A hora de fecho não pode ser anterior à hora de abertura.");
            return RedirectToAction(nameof(Index));
        }

        if ((closeTime - openTime).TotalHours < 1)
        {
            TempData.SetSwalError("O bar deve estar aberto pelo menos 1 hora.");
            return RedirectToAction(nameof(Index));
        }



        await _adminService.UpdateBarScheduleAsync(openTime.ToString(), closeTime.ToString());
        TempData.SetSwalSuccess($"Horario de funcionamento do Bar alterado com sucessso");
        return RedirectToAction(nameof(Index));


    }





}
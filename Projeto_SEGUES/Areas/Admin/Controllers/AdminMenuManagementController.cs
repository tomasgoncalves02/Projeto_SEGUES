using Microsoft.AspNetCore.Mvc;
using Microsoft.SqlServer.Server;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Services;
using static QuestPDF.Helpers.Colors;

[Area("Admin")]
public class AdminMenuManagementController : Controller
{
    private readonly IAdminService _adminService;

    public AdminMenuManagementController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<IActionResult> Index()
    {
        var model = new MenuManagementViewModel
        {
            CanteenUrl = await _adminService.GetCanteenMenuLinkAsync(),
            BarUrl = await _adminService.GetBarMenuLinkAsync()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SaveLinks(MenuManagementViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", model);
        await _adminService.UpdateMenuLinksAsync(model.CanteenUrl, model.BarUrl);
        TempData.SetSwalSuccess("Os links das ementas foram atualizados com sucesso!");
        return RedirectToAction("Index");
    }
}
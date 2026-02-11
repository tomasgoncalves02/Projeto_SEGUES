using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminCreateInternalAccountController : Controller
{
    private readonly IAdminService _adminService;
    
    public AdminCreateInternalAccountController(IAdminService adminService)
    {
        _adminService = adminService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = await _adminService.GetRolesForDropdownAsync();
        return View(new CreateInternalUserViewModel());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInternalUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _adminService.GetRolesForDropdownAsync();
            return View("Index", model);
        }
        
        var result = await _adminService.CreateInternalUserAsync(model);
        if (result.Succeeded)
        {
            TempData.SetSwalSuccess($"Conta criada para {model.FirstName}!");
            return RedirectToAction(nameof(Index));
        }
        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        ViewBag.Roles = await _adminService.GetRolesForDropdownAsync();
        return View("Index", model);
    }
}

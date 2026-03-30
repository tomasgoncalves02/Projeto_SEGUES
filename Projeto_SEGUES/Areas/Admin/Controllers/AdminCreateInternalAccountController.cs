using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin.Controllers;

/// <summary>
/// Controller responsible for creating internal accounts (staff/administrators) in the system.
/// </summary>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminCreateInternalAccountController : Controller
{
    private readonly IAdminService _adminService;

    /// <summary>
    /// Initializes a new instance of the controller with required services.
    /// </summary>
    public AdminCreateInternalAccountController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Displays the internal account creation form.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        return View();
    }

    /// <summary>
    /// Processes the creation of a new internal user and handles localized error messages.
    /// </summary>
    /// <param name="model">The view model for the internal user account.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInternalUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
            return View("Index", model);
        }
        
        var result = await _adminService.CreateInternalUserAsync(model);

        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction(nameof(Index));
        }
        TempData.SetSwalError(result.Message);
        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        return View("Index", model);
    }
}
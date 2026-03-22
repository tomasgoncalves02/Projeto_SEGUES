using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsible for creating internal accounts (staff/administrators) in the system.
/// </summary>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminCreateInternalAccountController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminCreateInternalAccountController> _logger;
    private readonly IStringLocalizer<Errors> _localizer;

    /// <summary>
    /// Initializes a new instance of the controller with required services.
    /// </summary>
    public AdminCreateInternalAccountController(
        IAdminService adminService,
        ILogger<AdminCreateInternalAccountController> logger,
        IStringLocalizer<Errors> localizer)
    {
        _adminService = adminService;
        _logger = logger;
        _localizer = localizer;
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
    /// Processes the form submission to create a new internal user.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInternalUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
            return View("Index", model);
        }

        try
        {
            var result = await _adminService.CreateInternalUserAsync(model);

            if (result.Success)
            {
                TempData.SetSwalSuccess($"Conta criada para {model.FirstName}!");
                return RedirectToAction(nameof(Index));
            }

            var errors = result.Message.Split("; ");
            foreach (var error in errors)
            {
                ModelState.AddModelError("", error);
            }
            ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogAppError(
                $"Erro na criação de conta ({model?.Email}): {ex.Message}",
                TableName.User,
                AppOperation.Create
            );
            var errorEnum = AppErrors.SendActivationEmailError;
            var translateMessage = _localizer[errorEnum.ToString()].Value;
            var finalMessage = $"{translateMessage} [Erro: {(int)errorEnum}]";

            TempData.SetSwalError(finalMessage);

            ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
            return View("Index", model);
        }
    }
}
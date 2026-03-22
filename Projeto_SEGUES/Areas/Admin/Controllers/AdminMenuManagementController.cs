using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using System.Diagnostics;

namespace Projeto_SEGUES.Areas.Admin.Controllers;

/// <summary>
/// Controller responsible for the administrative management of canteen and bar menu links.
/// </summary>
/// <remarks>
/// This controller allows the administration to dynamically update URLs pointing to weekly menus,
/// ensuring that users always have access to the latest information without requiring code changes.
/// </remarks>
[Area("Admin")]
public class AdminMenuManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminMenuManagementController> _logger;
    private readonly IStringLocalizer<Errors> _localizer;

    /// <summary>
    /// Initializes a new instance of the controller with the administrative service, logging, and localization.
    /// </summary>
    /// <param name="adminService">Service managing global settings and system link persistence.</param>
    /// <param name="logger">Logger for error tracking and auditing.</param>
    /// <param name="localizer">Localizer for translating error messages.</param>
    public AdminMenuManagementController(
        IAdminService adminService,
        ILogger<AdminMenuManagementController> logger,
        IStringLocalizer<Errors> localizer)
    {
        _adminService = adminService;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Displays the menu management page with the currently configured links.
    /// </summary>
    /// <returns>The index View populated with <see cref="MenuManagementViewModel"/> containing the current URLs.</returns>
    public async Task<IActionResult> Index()
    {
        try
        {
            var config = await _adminService.GetMenuLinksAsync();
            var model = new MenuManagementViewModel
            {
                CanteenUrl = config.CanteenMenuLink,
                BarUrl = config.BarMenuLink
            };
            return View(model);
        }
        catch (Exception ex)
        {
            // REDIRECT: Erro fatal ao carregar a página.
            _logger.LogError(ex, "Erro fatal ao carregar links do menu.");
            return RedirectToAction("Error", "Home", new
            {
                area = "",
                errorCode = (int)AppErrors.DatabaseQueryError
            });
        }
    }

    /// <summary>
    /// Processes the submission of new menu URLs.
    /// </summary>
    /// <param name="model">Model containing the validated new links.</param>
    /// <returns>
    /// Redirects to the index with a success message (SweetAlert) or 
    /// returns the View with validation errors if the model is invalid.
    /// </returns>
    /// <remarks>
    /// This method uses <see cref="IAdminService.UpdateMenuLinksAsync"/> to persist changes in the database.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLinks(MenuManagementViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", model);

        try
        {
            await _adminService.UpdateMenuLinksAsync(model.CanteenUrl, model.BarUrl);
            TempData.SetSwalSuccess("Os links das ementas foram atualizados com sucesso!");
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogAppError(
                $"Erro ao atualizar links do menu: {ex.Message}",
                TableName.AppConfig,
                AppOperation.Update
            );

            var errorEnum = AppErrors.DatabaseUpdateError;
            var translateMessage = _localizer[errorEnum.ToString()].Value;
            var finalMessage = $"{translateMessage} [Erro: {(int)errorEnum}]";

            TempData.SetSwalError(finalMessage);
            return View("Index", model);
        }
    }
}
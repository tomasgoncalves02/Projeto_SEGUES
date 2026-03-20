using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsible for creating internal accounts (staff/administrators) in the system.
/// </summary>
/// <remarks>
/// This controller manages the registration process for users who are not clients, 
/// including role assignment and activation email dispatch.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminCreateInternalAccountController : Controller
{
    private readonly IAdminService _adminService;

    /// <summary>
    /// Initializes a new instance of the controller with the administration service.
    /// </summary>
    /// <param name="adminService">Service interface containing user management logic.</param>
    public AdminCreateInternalAccountController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Displays the internal account creation form.
    /// </summary>
    /// <returns>The index View with the list of available roles in the ViewBag.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        return View();
    }

    /// <summary>
    /// Processes the form submission to create a new internal user.
    /// </summary>
    /// <param name="model">Data model containing the new user's information.</param>
    /// <returns>
    /// Redirects to Index on success or returns the View with error messages on failure.
    /// </returns>
    /// <remarks>
    /// Validates the model state, attempts user creation via the service, and handles exceptions related to email sending.
    /// </remarks>
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
                ModelState.AddModelError("", error);
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Erro ao enviar e-mail de ativação. Verifique a sua conexão à Internet.");
            TempData.SetSwalError("Falha na conexão: O e-mail não pode ser enviado, por isso a conta não foi criada.");
        }

        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        return View("Index", model);
    }
}
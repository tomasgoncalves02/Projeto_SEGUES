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
        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        return View();
    }

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

            if (result.Succeeded)
            {
                TempData.SetSwalSuccess($"Conta criada para {model.FirstName}!");
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Erro ao enviar e-mail de ativação. Verifique a sua conexão à Internet.");
            TempData.SetSwalError("Falha na conexão: O e-mail não pode ser enviado, por isso a conta não foi criada.");
        }

        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        return View("Index", model);
    }
}

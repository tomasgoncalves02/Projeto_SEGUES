using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminUserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;
    
    public AdminUserManagementController(UserManager<AppUser> userManager, IAdminService adminService)
    {
        _userManager = userManager;
        _adminService = adminService;
    }
    
    public async Task<IActionResult> Index(string? searchString, string? roleFilter)
    {
        var users = await _adminService.GetFilteredUsersAsync(searchString, roleFilter);
        ViewData["SearchString"] = searchString;
        ViewData["CurrentRole"] = roleFilter;
        
        ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
        return View(users);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.Users.Include(u => u.UserCategory).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        
        var roles = await _userManager.GetRolesAsync(user);
        var allRoles = await _adminService.GetAllRolesForDropdownAsync();
        ViewBag.UserRole = allRoles.Find(r => r.Value == roles.First())?.Text;
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();

        return View(new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Gender = user.Gender,
            BirthDate =  user.BirthDate,
            Balance = user.Balance,
            Role = roles.FirstOrDefault() ?? "Client",
            Category = user.UserCategory.Name
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
            ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.Balance = model.Balance;
        user.Gender = model.Gender;
        user.BirthDate = model.BirthDate;
        user.UserCategory = await _adminService.GetCategoryByNameAsync(model.Category);

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var oldRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            TempData.SetSwalSuccess("Utilizador atualizado.");
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData.SetSwalError("Utilizador não encontrado.");
            return RedirectToAction(nameof(Index));
        }
        
        if (user.UserName == User.Identity?.Name)
        {
            TempData.SetSwalError("Não podes apagar a tua própria conta.");
            return RedirectToAction(nameof(Index));
        }
        
        user.Status = UserStatus.Inactive;
        // Lockout the user (prevents login immediately)
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        
        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            TempData.SetSwalSuccess($"O utilizador {user.FirstName} foi desativado com sucesso.");
        }
        else
        {
            TempData.SetSwalError("Erro ao desativar utilizador.");
        }
        return RedirectToAction(nameof(Index));
    }
}
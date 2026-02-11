using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
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
    
    public async Task<IActionResult> Index(string roleFilter, string searchString)
    {
        var users = await _adminService.GetFilteredUsersAsync(roleFilter, searchString);
        ViewData["CurrentFilter"] = roleFilter;
        ViewData["SearchString"] = searchString;
        return View(users);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.Users.Include(u => u.UserCategory).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.UserRole = roles.FirstOrDefault() ?? "Nenhum";
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = await _adminService.GetRolesForDropdownAsync();

        return View(new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Gender = user.Gender,
            BirthDate =  user.BirthDate,
            Balance = user.Balance,
            Role = roles.FirstOrDefault() ?? "Client"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _adminService.GetRolesForDropdownAsync();
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
        ViewBag.Roles = await _adminService.GetRolesForDropdownAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user == null ? NotFound() : View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return RedirectToAction(nameof(Index));
        
        if (user.UserName == User.Identity!.Name)
        {
            TempData.SetSwalError("Não podes apagar a tua própria conta.");
            return RedirectToAction(nameof(Index));
        }
        await _userManager.DeleteAsync(user);
        TempData.SetSwalSuccess("Utilizador removido.");
        return RedirectToAction(nameof(Index));
    }
}
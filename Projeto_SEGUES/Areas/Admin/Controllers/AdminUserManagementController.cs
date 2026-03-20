using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsible for user management within the administrative area.
/// </summary>
/// <remarks>
/// This controller allows listing, detailing, editing, activating, and deactivating user accounts, 
/// in addition to managing permissions (roles), categories, and viewing staff audit logs.
/// </remarks>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminUserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the controller with Identity, administration, and data context services.
    /// </summary>
    /// <param name="userManager">Native ASP.NET Identity service for user management.</param>
    /// <param name="adminService">Custom service containing administrative business logic.</param>
    /// <param name="context">Database context for direct queries (e.g., Logs).</param>
    public AdminUserManagementController(UserManager<AppUser> userManager, IAdminService adminService, AppDbContext context)
    {
        _userManager = userManager;
        _adminService = adminService;
        _context = context;
    }

    /// <summary>
    /// Lists the system users with support for search and filters.
    /// </summary>
    /// <param name="searchString">Search term (name or email).</param>
    /// <param name="roleFilter">Filter by role type (Admin, Staff, Client).</param>
    /// <param name="categoryFilter">Filter by user category (Student, Faculty, etc.).</param>
    /// <returns>The index View with the filtered collection of users.</returns>
    public async Task<IActionResult> Index(string? searchString, string? roleFilter, string? categoryFilter)
    {
        var users = await _adminService.GetFilteredUsersAsync(searchString, roleFilter, categoryFilter);
        ViewData["SearchString"] = searchString;
        ViewData["CurrentRole"] = roleFilter;
        ViewData["CurrentCategory"] = categoryFilter;

        ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
        return View(users);
    }

    /// <summary>
    /// Displays the complete details of a specific user.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the user.</param>
    /// <returns>The details View or NotFound if the user does not exist.</returns>
    /// <remarks>
    /// Translates enums and states to Portuguese and defines dynamic CSS classes for the interface.
    /// </remarks>
    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.Users
            .Include(u => u.UserCategory)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var userRoleRaw = roles.FirstOrDefault() ?? "Client";
        var allRoles = await _adminService.GetAllRolesForDropdownAsync();

        ViewBag.UserRole = allRoles.Find(r => r.Value == userRoleRaw)?.Text ?? userRoleRaw;
        ViewBag.UserRoleRaw = userRoleRaw;

        ViewBag.GenderPT = user.Gender switch
        {
            Gender.Male => "Masculino",
            Gender.Female => "Feminino",
            Gender.Other => "Outro",
            _ => "Não especificado"
        };

        ViewBag.StatusPT = user.Status == UserStatus.Active ? "ATIVO" : "INATIVO";
        ViewBag.StatusClass = user.Status == UserStatus.Active ? "bg-success" : "bg-danger";
        ViewBag.StatusIcon = user.Status == UserStatus.Active ? "bi-check-circle" : "bi-x-circle";

        return View(user);
    }

    /// <summary>
    /// Displays the user edit form.
    /// </summary>
    /// <param name="id">The ID of the user to edit.</param>
    /// <returns>View with the pre-filled ViewModel for editing.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();

        return View(new EditUserViewModelAdmin
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Gender = user.Gender,
            BirthDate = user.BirthDate,
            Balance = user.Balance,
            Role = roles.FirstOrDefault() ?? "Client",
            Category = user.UserCategory.Name
        });
    }

    /// <summary>
    /// Processes changes to a user's data, category, and role.
    /// </summary>
    /// <param name="model">ViewModel with updated data.</param>
    /// <returns>Redirects to Index upon success.</returns>
    /// <remarks>
    /// If the Role is changed, the SecurityStamp is updated to force a refresh of the user's claims.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModelAdmin model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
            ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        string? pendingEmail = null;
        if (model.Email != user.Email)
        {
            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists != null)
            {
                ModelState.AddModelError("Email", "Este email já está em uso.");
                ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
                ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
                return View(model);
            }
            pendingEmail = model.Email;
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
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


            await _userManager.UpdateSecurityStampAsync(user);

            bool emailSentSuccessfully = false;

            if (!string.IsNullOrEmpty(pendingEmail))
            {
                try
                {
                    await _adminService.RequestEmailChangeAsync(user, pendingEmail, Url, Request.Scheme);
                    emailSentSuccessfully = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro SMTP: {ex.Message}");
                    TempData.SetSwalWarning("Dados gravados, mas não foi possível enviar o email de confirmação. Verifique o servidor SMTP.");
                    return RedirectToAction(nameof(Index));
                }
            }

            if (emailSentSuccessfully)
            {
                TempData.SetSwalInfo("Utilizador atualizado! Foi enviado um link de confirmação para o novo email.");
            }
            else
            {
                TempData.SetSwalSuccess("Utilizador atualizado com sucesso.");
            }

            return RedirectToAction(nameof(Index));
        }
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
        return View(model);
    }

    /// <summary>
    /// Deactivates a user, preventing login through permanent Lockout.
    /// </summary>
    /// <param name="id">The ID of the user to deactivate.</param>
    /// <returns>Redirects to Index with the result of the operation.</returns>
    /// <remarks>Prevents the administrator from deactivating their own account.</remarks>
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

    /// <summary>
    /// Reactivates a previously deactivated user account.
    /// </summary>
    /// <param name="id">The ID of the user to activate.</param>
    /// <returns>Redirects to the user's Details View.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData.SetSwalError("Utilizador não encontrado.");
            return RedirectToAction(nameof(Index));
        }

        user.Status = UserStatus.Active;
        await _userManager.SetLockoutEndDateAsync(user, null);

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData.SetSwalSuccess($"A conta de {user.FirstName} foi reativada.");
        }
        else
        {
            TempData.SetSwalError("Erro ao reativar utilizador.");
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Displays the selection page for different types of logs.
    /// </summary>
    /// <returns>The log selection View.</returns>
    public IActionResult UserLogSelection()
    {
        return View();
    }

    /// <summary>
    /// Lists the activity logs performed by Staff members (internal audit).
    /// </summary>
    /// <param name="search">Search term (username or message content).</param>
    /// <param name="date">Filter by a specific date.</param>
    /// <returns>The View with the list of logs sorted by descending date.</returns>
    public async Task<IActionResult> StaffLog(string search, string date)
    {
        var query = _context.UserLog
            .Include(l => l.AppUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l => l.AppUser.UserName.Contains(search) || l.Message.Contains(search));
        }

        if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime parsedDate))
        {
            query = query.Where(l => l.TimeStamp.Date == parsedDate.Date);
        }

        var logs = await query.OrderByDescending(l => l.TimeStamp).ToListAsync();

        return View(logs);
    }
}
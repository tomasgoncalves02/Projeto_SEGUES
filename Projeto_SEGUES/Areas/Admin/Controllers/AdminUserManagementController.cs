using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using AppErrors = Projeto_SEGUES.Models.Enums.AppErrors;

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
    private readonly IUserService _userService;
    private readonly AppDbContext _context;
    private readonly ILogger<AdminUserManagementController> _logger;

    /// <summary>
    /// Initializes a new instance of the controller with Identity, administration, and data context services.
    /// </summary>
    /// <param name="userManager">Native ASP.NET Identity service for user management.</param>
    /// <param name="adminService">Custom service containing administrative business logic.</param>
    /// <param name="context">Database context for direct queries (e.g., Logs).</param>
    public AdminUserManagementController(UserManager<AppUser> userManager, IAdminService adminService, IUserService userService, AppDbContext context, ILogger<AdminUserManagementController> logger)
    {
        _userManager = userManager;
        _adminService = adminService;
        _userService = userService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lists the system users with support for search and filters.
    /// </summary>
    /// <returns>The index View with the filtered collection of users.</returns>
    public async Task<IActionResult> Index()
    {
        AdminUserManagementViewModel vm = new AdminUserManagementViewModel
        {
            Roles = await _adminService.GetAllRolesForDropdownAsync(),
            Categories = await _adminService.GetAllCategoriesForDropdownAsync(),
            SearchModel = new UserSearchViewModel
            {
                Results = await _adminService.GetFilteredUsersAsync()
            }
        };
        
        return View(vm);
    }
    
    /// <summary>
    /// Returns the filtered user table rows via HTMX.
    /// <param name="model">The search and filter parameters bound from the request.</param>
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFilteredUsers([Bind(Prefix = "SearchModel")] UserSearchViewModel model)
    {
        model.Results = await _adminService.GetFilteredUsersAsync(
            model.SearchString, 
            model.RoleFilter, 
            model.CategoryFilter
        );

        return PartialView("_UserTableRowsPartial", model.Results);
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

        if (user == null)
        {
            _logger.LogAppError(AppErrors.UserNotFound, TableName.User, AppOperation.Read); 
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.UserNotFound });
        }
        
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Client";
        var roleDisplay = (await _adminService.GetRoleByNameAsync(role))?.DisplayName ?? role;
        
        UserDto dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            GenderDisplay = user.Gender.ToDisplayName(),
            BirthDateDisplay = user.BirthDate.ToString("dd/MM/yyyy"),
            CreationDateDisplay = user.CreationDate.ToString("dd/MM/yyyy"),
            BalanceFormatted = user.Balance.ToString("C"),
            
            RoleName = roleDisplay,
            RoleBadgeClass = role.ToBadgeClass(),
            
            CategoryName = user.UserCategory.Name,
            CategoryBadgeClass = user.UserCategory.Name.ToBadgeClass(),
            
            IsActive = user.Status == UserStatus.Active,
        };

        return View(dto);
    }

    /// <summary>
    /// Displays the user edit form.
    /// </summary>
    /// <param name="id">The ID of the user to edit.</param>
    /// <returns>View with the pre-filled ViewModel for editing.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetUserForEditAsync(id);
        if (user == null)
        {
            _logger.LogAppError(AppErrors.UserNotFound, TableName.User, AppOperation.Read);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.UserNotFound });
        }
        var roles = await _userManager.GetRolesAsync(user);
        var model = new EditUserAdminViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            Gender = user.Gender,
            BirthDate = user.BirthDate,
            Balance = user.Balance,
            Role = roles.FirstOrDefault() ?? "Client",
            Category = user.UserCategory.Name,
            FiscalNumber = user.FiscalNumber,
            Address = user.Address,
            City = user.City,
            PostalCode = user.PostalCode?.Code,
            StudentNumber = (user is Student student) ? student.StudentNumber : null,
            RoleDescription = (user is Employee employee) ? employee.RoleDescription : null,
            SchoolId = (user is Student studentUser) ? studentUser.School?.Id : (user is Employee employeeUser) ? employeeUser.School?.Id : null,
            RolesList = await _adminService.GetAllRolesForDropdownAsync(),
            CategoriesList = await _adminService.GetAllCategoriesForDropdownAsync(),
            SchoolsList = await _userService.GetSchoolsAsync()
        };
        
        return View(model);
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
    public async Task<IActionResult> Edit(EditUserAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RolesList = await _adminService.GetAllRolesForDropdownAsync();
            model.CategoriesList = await _adminService.GetAllCategoriesForDropdownAsync();
            model.SchoolsList = await _userService.GetSchoolsAsync();
            return View(model);
        }
        
        var user = await _userService.GetUserForEditAsync(model.Id);
        if (user == null)
        {
            _logger.LogAppError(AppErrors.UserNotFound, TableName.User, AppOperation.Read);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.UserNotFound });
        }
        
        var result = await _adminService.UpdateUserAdminAsync(user, model, Url, Request.Scheme);
        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction(nameof(Index));
        }
        
        TempData.SetSwalError(result.Message);
        model.RolesList = await _adminService.GetAllRolesForDropdownAsync();
        model.CategoriesList = await _adminService.GetAllCategoriesForDropdownAsync();
        model.SchoolsList = await _userService.GetSchoolsAsync();
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
            _logger.LogAppError(AppErrors.UserNotFound, TableName.User, AppOperation.Update);
            TempData.SetSwalError("O utilizador indicado não foi encontrado.");
            return RedirectToAction(nameof(Index));
        }

        if (user.UserName == User.Identity?.Name)
        {
            TempData.SetSwalError("Medida de segurança: Não podes desativar a tua própria conta.");
            return RedirectToAction(nameof(Index));
        }

        user.Status = UserStatus.Inactive;
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await _userManager.UpdateAsync(user);

        TempData.SetSwalSuccess($"A conta de {user.FirstName} foi desativada.");
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
            _logger.LogAppError(AppErrors.UserNotFound, TableName.User, AppOperation.Update);
            TempData.SetSwalError("Utilizador não encontrado.");
            return RedirectToAction(nameof(Index));
        }

        user.Status = UserStatus.Active;
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.UpdateAsync(user);

        TempData.SetSwalSuccess($"A conta de {user.FirstName} está novamente ativa.");
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
        var logs = _adminService.GetStaffLogFilteredAsync(search, date);
        return View(logs);
    }
    
}
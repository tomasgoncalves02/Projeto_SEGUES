using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.User.Controllers;

/// <summary>
/// Controller responsible for managing the profile and personal data of the authenticated user.
/// </summary>
/// <remarks>
/// This controller handles the retrieval and updating of user information, 
/// supporting polymorphic data structures for specific roles like Students and Employees.
/// </remarks>
[Area("User")]
[Authorize]
public class UserController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/>.
    /// </summary>
    public UserController(UserManager<AppUser> userManager, RoleManager<Role> roleManager, IUserService userService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userService = userService;
    }

    /// <summary>
    /// Displays the user profile page with current information.
    /// </summary>
    /// <returns>A View containing the <see cref="EditUserViewModel"/> populated with user data.</returns>
    /// <remarks>
    /// Uses Eager Loading to fetch related data (Category, School, Postal Code) 
    /// and performs type checking to map specific attributes based on the user's concrete class.
    /// </remarks>
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.Users
            .Include(u => u.UserCategory)
            .Include(u => u.PostalCode)
            .Include(u => (u as Student)!.School)
            .Include(u => (u as Employee)!.School)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return Challenge();

        var roleString = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Client";
        var role = await _roleManager.FindByNameAsync(roleString);
        ViewBag.Schools = await _userService.GetSchoolsAsync();

        var editUserViewModel = new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            Email = user.Email!,
            Gender = user.Gender,
            FiscalNumber = user.FiscalNumber,
            Address = user.Address,
            City = user.City,
            Role = role!,
            Category = user.UserCategory.Name,
            StudentNumber = user is Student student ? student.StudentNumber : null,
            RoleDescription = user is Employee employee ? employee.RoleDescription : null,
            SchoolId = user switch
            {
                Student student2 => student2.School?.Id,
                Employee employee2 => employee2.School?.Id,
                _ => null
            },
            PostalCode = user.PostalCode?.Code
        };

        return View(editUserViewModel);
    }

    /// <summary>
    /// Processes the user profile update request.
    /// </summary>
    /// <param name="model">The ViewModel containing the updated profile data.</param>
    /// <returns>
    /// A redirect to the Index on success, or the same view with validation errors on failure.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(EditUserViewModel model)
    {
        var user = await _userManager.Users
            .Include(u => u.UserCategory)
            .Include(u => u.PostalCode)
            .Include(u => (u as Student)!.School)
            .Include(u => (u as Employee)!.School)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            // Reload metadata required for the View
            ViewBag.Schools = await _userService.GetSchoolsAsync();
            model.Email = user.Email!;
            model.Category = user.UserCategory.Name;
            var roleString = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Client";
            var role = await _roleManager.FindByNameAsync(roleString);
            model.Role = role!;
            if (user is Employee emp) model.RoleDescription = emp.RoleDescription;

            TempData.SetSwalError("Por favor, verifique os dados preenchidos.");
            return View(nameof(Index), model);
        }

        // Delegating the update logic to the Service layer for better testability
        var result = await _userService.UpdateUserProfileAsync(user, model);

        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction(nameof(Index));
        }

        TempData.SetSwalError(result.Message);

        ViewBag.Schools = await _userService.GetSchoolsAsync();
        return View(nameof(Index), model);
    }
}
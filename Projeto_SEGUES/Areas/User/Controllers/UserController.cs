using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class UserController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IUserService _userService;

    public UserController(UserManager<AppUser> userManager, RoleManager<Role> roleManager, IUserService userService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userService = userService;
    }

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
            // Reload data
            ViewBag.Schools = await _userService.GetSchoolsAsync();
            model.Email = user.Email!;
            model.Category = user.UserCategory.Name;
            var roleString = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Client";
            var role = await _roleManager.FindByNameAsync(roleString);
            model.Role = role!;
            if (user is Employee emp) model.RoleDescription = emp.RoleDescription;
            
            // Reload with filled data
            TempData.SetSwalError("Por favor, verifique os dados preenchidos.");
            return View(nameof(Index), model);
        }
        
        var result = await _userService.UpdateUserProfileAsync(user, model);
        
        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction(nameof(Index));
        }

        TempData.SetSwalError(result.Message);
            
        // Reload with filled data
        ViewBag.Schools = await _userService.GetSchoolsAsync();
        return View(nameof(Index), model);
    }
}
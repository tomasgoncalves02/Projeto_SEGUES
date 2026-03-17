using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IUserService _userService;

    public UserController(UserManager<AppUser> userManager, IUserService userService)
    {
        _userManager = userManager;
        _userService = userService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        ViewBag.Email = user.Email;

        var roles = await _userManager.GetRolesAsync(user);
        var userRole = roles.FirstOrDefault() ?? "Client";

        var editUserViewModel = new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            Gender = user.Gender,
            FiscalNumber = user.FiscalNumber,
            Address = user.Address,
            City = user.City,
            Role = userRole,
            Category = user.UserCategory.Name,
            StudentNumber = user is Student student ? student.StudentNumber : null,
            RoleDescription = user is Employee employee ? employee.RoleDescription : null
        };
        return View(editUserViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(EditUserViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            TempData.SetSwalError("Utilizador não encontrado.");
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join("<br>", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { success = false, message = errors });
        }

        // Update AppUser general fields
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.BirthDate = model.BirthDate;
        user.Gender = model.Gender;
        user.FiscalNumber = model.FiscalNumber;
        user.Address = model.Address;
        user.City = model.City;

        // Update specific fields based on user type
        if (user is Student student)
        {
            student.StudentNumber = model.StudentNumber;
        }
        else if (user is Employee employee)
        {
            employee.RoleDescription = model.RoleDescription;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var identityErrors = string.Join("<br>", result.Errors.Select(e => e.Description));
            return BadRequest(new { success = false, message = $"Erro ao atualizar perfil: {identityErrors}" });
        }

        TempData.SetSwalSuccess("Perfil atualizado com sucesso!");
        return Ok(new { success = true, message = "Perfil atualizado com sucesso!" });
    }
}
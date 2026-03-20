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

        var editUserViewModel = new EditUserViewModelAdmin
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            Gender = user.Gender,
            /*FiscalNumber = user.FiscalNumber,
            Address = user.Address,
            City = user.City,*/
            Role = userRole,
            Category = user.UserCategory?.Name ?? "Sem Categoria",
            //StudentNumber = user is Student student ? student.StudentNumber : null,
            RoleDescription = user is Employee employee ? employee.RoleDescription : null
        };
        return View(editUserViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData.SetSwalError("Dados inválidos.");
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Gender = model.Gender;
        user.BirthDate = model.BirthDate;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData.SetSwalSuccess("Perfil atualizado com sucesso!");
            return RedirectToAction(nameof(Index));
        }

        TempData.SetSwalError("Erro ao gravar.");
        return RedirectToAction(nameof(Index));
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class UserController : Controller
{
    private readonly IAdminService _adminService;
    private readonly UserManager<AppUser> _userManager;

    public UserController(IAdminService adminService, UserManager<AppUser> userManager)
    {
        _adminService = adminService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> UpdateType(string key, string value)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }


        switch (key)
        {
            case "name":
                user.FirstName = value;
                break;

            case "lastname":
                user.LastName = value;
                break;

            case "email":
                user.Email = value;
                user.UserName = value;
                break;

            case "birthDate":
                if (DateTime.TryParse(value, out var date))
                    user.BirthDate = date;
                break;

            case "genre":
                if (Enum.TryParse<Gender>(value, out var gender))
                    user.Gender = gender;
                break;

            case "password":
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, value);
                break;

            default:
                return BadRequest(new { success = false, message = "Campo inválido." });
               
        }
        await _userManager.UpdateAsync(user);
        return Ok(new { success = true });
    }

    [HttpGet]
    public IActionResult GetGenders()
    {
        var genders = Enum.GetValues<Gender>()
            .Select(g => new { value = g.ToString(), text = g.ToDisplayName() })
            .ToList();
        return Json(genders);
    }




}
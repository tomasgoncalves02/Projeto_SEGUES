using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Net.Mail;
using System.Text.RegularExpressions;

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
    [ValidateAntiForgeryToken]
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
                if (string.IsNullOrWhiteSpace(value))
                    return BadRequest(new { success = false, message = "O Nome não pode estar vazio." });

                if (value.Length < 2 || value.Length > 50)
                    return BadRequest(new { success = false, message = "O Nome deve ter entre 2 e 50 letras." });

                if (!Regex.IsMatch(value, @"^[a-zA-Z\u00C0-\u00FF\s]*$"))
                    return BadRequest(new { success = false, message = "O Nome não pode conter números nem símbolos." });

                user.FirstName = value;
                break;

            case "lastname":

                if (string.IsNullOrWhiteSpace(value))
                    return BadRequest(new { success = false, message = "O Apelido não pode estar vazio." });

                if (value.Length < 2 || value.Length > 50)
                    return BadRequest(new { success = false, message = "O Apelido deve ter entre 2 e 50 letras." });

                if (!Regex.IsMatch(value, @"^[a-zA-Z\u00C0-\u00FF\s]*$"))
                    return BadRequest(new { success = false, message = "O Apelido não pode conter números nem símbolos." });

                user.LastName = value;
                break;

            case "email":
                if (string.IsNullOrWhiteSpace(value))
                    return BadRequest(new { success = false, message = "O email é obrigatório." });

                try
                {
                    var addr = new MailAddress(value);
                    if (addr.Address != value) throw new Exception();
                }
                catch
                {
                    return BadRequest(new { success = false, message = "Email inválido." });
                }

                user.Email = value;
                user.UserName = value;
                break;

            case "birthDate":
                if (!DateTime.TryParse(value, out var date))
                    return BadRequest(new { success = false, message = "Data de nascimento inválida." });

                if (date > DateTime.Now.AddYears(-18))
                    return BadRequest(new { success = false, message = "Deve ter pelo menos 18 anos." });

                if (date < DateTime.Now.AddYears(-120))
                    return BadRequest(new { sucess = false, message = "Deve ter menos de 120 anos" });

                user.BirthDate = date;
                break;

            case "genre":

                if (!Enum.TryParse<Gender>(value, out var gender))
                    return BadRequest(new { success = false, message = "Género inválido." });

                user.Gender = gender;
                break;

            default:
                return BadRequest(new { success = false, message = "Campo inválido." });
               
        }
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { success = false, message = "Erro ao atualizar utilizador." });

        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$");

        if (user == null) return NotFound();

        if(currentPassword == newPassword)
          return BadRequest(new { success = false, message = "Não pode usar a mesma password outra vez" });

        if (!passwordRegex.IsMatch(newPassword))
            return BadRequest(new { success = false, message = "A password deve ter mínimo 12 caracteres, uma maiúscula, uma minúscula, um número e um símbolo (@$!%*?&)." });
        
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (result.Succeeded)
            return Ok(new { success = true });

        return BadRequest(new { success = false, message = "Password atual incorreta." });
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
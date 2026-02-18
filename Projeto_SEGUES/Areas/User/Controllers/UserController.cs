using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class UserController : Controller
{
    private readonly IAdminService _adminService;

    public UserController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
        return View();
    }
}
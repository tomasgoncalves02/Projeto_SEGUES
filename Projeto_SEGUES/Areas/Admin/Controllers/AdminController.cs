using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Admin.Controllers;

/// <summary>
/// Primary controller for the system's administrative area.
/// </summary>
/// <remarks>
/// This controller serves as the entry point for global management functionalities.
/// Access is restricted exclusively to users with Administrator privileges.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminController : Controller
{
    /// <summary>
    /// Displays the administrative dashboard or the main landing page of the admin panel.
    /// </summary>
    /// <returns>The main administrative index View.</returns>
    public IActionResult Index()
    {
        return View();
    }
}
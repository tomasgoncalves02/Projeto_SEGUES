using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsible for managing the administrative bar and back-office navigation.
/// </summary>
/// <remarks>
/// Access to this controller is restricted to users with the "Admin" role. 
/// It is a core component of the SEGUES project's administrative area.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminBarManagementController : Controller
{
    /// <summary>
    /// Displays the main management page for the administrative bar.
    /// </summary>
    /// <returns>
    /// Returns the View corresponding to the administrative bar control panel.
    /// </returns>
    public IActionResult Index()
    {
        return View();
    }
}
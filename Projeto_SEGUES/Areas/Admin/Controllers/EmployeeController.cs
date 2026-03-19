using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsible for the primary interface for Employees.
/// </summary>
/// <remarks>
/// This controller serves as the entry point for users with "Admin" or "Employee" roles,
/// providing access to daily management tools within the administrative area.
/// </remarks>
[Authorize(Roles = "Admin,Employee")]
[Area("Admin")]
public class EmployeeController : Controller
{
    /// <summary>
    /// Displays the employee's home page or dashboard.
    /// </summary>
    /// <returns>The View corresponding to the employee area index.</returns>
    public IActionResult Index()
    {
        return View();
    }
}
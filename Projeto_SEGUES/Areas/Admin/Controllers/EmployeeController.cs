using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsável pela interface principal dos funcionários (Employees).
/// </summary>
/// <remarks>
/// Este controlador serve como ponto de entrada para utilizadores com as funções de "Admin" ou "Employee",
/// fornecendo acesso às ferramentas de gestão diária dentro da área administrativa.
/// </remarks>
[Authorize(Roles = "Admin,Employee")]
[Area("Admin")]
public class EmployeeController : Controller
{
    /// <summary>
    /// Apresenta a página inicial ou o dashboard do funcionário.
    /// </summary>
    /// <returns>A View correspondente ao índice da área do funcionário.</returns>
    public IActionResult Index()
    {
        return View();
    }
}
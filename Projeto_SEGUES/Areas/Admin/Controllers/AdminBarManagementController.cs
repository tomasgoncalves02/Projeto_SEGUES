using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsável pela gestão da barra administrativa e navegação do back-office.
/// </summary>
/// <remarks>
/// Este controlador está restrito a utilizadores com a role "Admin".
/// Faz parte da Área administrativa do projeto SEGUES.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminBarManagementController : Controller
{
    /// <summary>
    /// Apresenta a página principal da gestão da barra administrativa.
    /// </summary>
    /// <returns>
    /// Devolve a View correspondente ao painel de controlo da barra de administração.
    /// </returns>
    public IActionResult Index()
    {
        return View();
    }
}
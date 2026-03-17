using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller principal da área administrativa do sistema.
/// </summary>
/// <remarks>
/// Este controlador serve como ponto de entrada para as funcionalidades de gestão global,
/// acessível apenas por utilizadores com privilégios de Administrador.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminController : Controller
{
    /// <summary>
    /// Apresenta o dashboard ou a página inicial do painel administrativo.
    /// </summary>
    /// <returns>A View principal do índice administrativo.</returns>
    public IActionResult Index()
    {
        return View();
    }
}
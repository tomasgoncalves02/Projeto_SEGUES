using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Report;

/// <summary>
/// Controller responsável pela geração e visualização de relatórios do sistema.
/// </summary>
/// <remarks>
/// Este controlador pertence à área "Report" e permite aos utilizadores autenticados 
/// acederem a dados estatísticos, históricos de consumo ou exportações de documentos.
/// </remarks>
[Authorize]
[Area("Report")]
public class ReportController : Controller
{
    /// <summary>
    /// Apresenta a página principal do módulo de relatórios.
    /// </summary>
    /// <returns>A View correspondente ao índice de relatórios e estatísticas.</returns>
    /// <remarks>
    /// Serve como o painel central onde o utilizador pode escolher o tipo de relatório a gerar.
    /// </remarks>
    public IActionResult Index()
    {
        return View();
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Report.Controllers;

/// <summary>
/// Controller responsável pela geração de relatórios de movimentos financeiros do utilizador.
/// </summary>
/// <remarks>
/// Este controlador gere a consulta e filtragem do histórico de transações, permitindo ao utilizador
/// auditar os seus carregamentos de saldo e débitos efetuados no sistema SEGUES.
/// </remarks>
[Authorize]
[Area("Report")]
public class ReportTransactionController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    /// <summary>
    /// Inicializa uma nova instância do controlador de relatórios de transações.
    /// </summary>
    /// <param name="userManager">Gestor de utilizadores para obter o contexto do utilizador autenticado.</param>
    /// <param name="context">Contexto da base de dados para acesso à tabela de transações.</param>
    public ReportTransactionController(UserManager<AppUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    /// <summary>
    /// Apresenta a página principal do histórico de movimentos financeiros.
    /// </summary>
    /// <returns>A View de índice populada com a lista de transações do utilizador logado.</returns>
    /// <remarks>
    /// As transações são carregadas com Eager Loading para a entidade <see cref="AppUser"/> 
    /// e ordenadas por data de criação descendente.
    /// </remarks>
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var transactions = await _context.Transaction
            .Include(t => t.User)
            .Where(t => t.User.Id == user.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(transactions);
    }

    /// <summary>
    /// Filtra o histórico de transações com base em critérios de pesquisa, tipo e data.
    /// </summary>
    /// <param name="searchString">Termo de pesquisa para a descrição ou referência da transação.</param>
    /// <param name="typeFilter">Filtro para movimentos de "Entrada" (positivos) ou "Saida" (negativos).</param>
    /// <param name="dateFilter">Data mínima para a inclusão de resultados no relatório.</param>
    /// <returns>Uma PartialView contendo as linhas da tabela filtradas.</returns>
    /// <remarks>
    /// Este método utiliza <see cref="IQueryable"/> para construir a consulta de forma eficiente 
    /// antes da execução na base de dados. É ideal para integração com componentes HTMX.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetFilteredBalance(string searchString, string typeFilter, DateTime? dateFilter)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Inicialização da Query base associada ao utilizador
        var query = _context.Transaction
            .Include(t => t.User)
            .Where(t => t.User.Id == user.Id)
            .AsQueryable();

        // Filtragem por texto (Case-insensitive)
        if (!string.IsNullOrEmpty(searchString))
        {
            var search = searchString.ToLower();
            query = query.Where(t => t.Description!.ToLower().Contains(search) ||
                                     t.Reference.ToLower().Contains(search));
        }

        // Filtragem por fluxo financeiro
        if (!string.IsNullOrEmpty(typeFilter))
        {
            if (typeFilter == "Entrada") query = query.Where(t => t.Amount > 0);
            else if (typeFilter == "Saida") query = query.Where(t => t.Amount < 0);
        }

        // Filtragem cronológica
        if (dateFilter.HasValue)
        {
            query = query.Where(t => t.CreatedAt.Date >= dateFilter.Value.Date);
        }

        var result = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        return PartialView("_BalanceHistoryRows", result);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Payment;

namespace Projeto_SEGUES.Areas.Report.Controllers;

[Authorize]
[Area("Report")]
public class ReportTransactionController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    public ReportTransactionController(UserManager<AppUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

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

    [HttpGet]
    public async Task<IActionResult> GetFilteredBalance(string searchString, string typeFilter, DateTime? dateFilter)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // 1. Começamos a query incluindo o User
        var query = _context.Transaction
            .Include(t => t.User)
            .Where(t => t.User.Id == user.Id)
            .AsQueryable();

        // 2. Filtro de Pesquisa (Ajustado para ser case-insensitive se necessário)
        if (!string.IsNullOrEmpty(searchString))
        {
            var search = searchString.ToLower();
            query = query.Where(t => t.Description!.ToLower().Contains(search) ||
                                     t.Reference.ToLower().Contains(search));
        }

        // 3. Filtro de Tipo (Entrada/Saída)
        if (!string.IsNullOrEmpty(typeFilter))
        {
            // Nota: Garante que os valores no <select> da View são exatamente "Entrada" e "Saida"
            if (typeFilter == "Entrada") query = query.Where(t => t.Amount > 0);
            else if (typeFilter == "Saida") query = query.Where(t => t.Amount < 0);
        }

        // 4. Filtro de Data
        if (dateFilter.HasValue)
        {
            query = query.Where(t => t.CreatedAt.Date >= dateFilter.Value.Date);
        }

        var result = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        // 5. IMPORTANTE: Se usares HTMX, precisas de retornar a Partial
        return PartialView("_BalanceHistoryRows", result);
    }
}
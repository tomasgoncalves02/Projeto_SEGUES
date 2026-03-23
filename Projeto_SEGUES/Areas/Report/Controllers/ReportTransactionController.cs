using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;

namespace Projeto_SEGUES.Areas.Report.Controllers;

/// <summary>
/// Controller responsible for generating user financial movement reports.
/// </summary>
/// <remarks>
/// This controller manages the query and filtering of transaction history, allowing users
/// to audit their top-ups and debits performed within the SEGUES system.
/// </remarks>
[Authorize]
[Area("Report")]
public class ReportTransactionController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;
    private readonly ILogger<ReportTransactionController> _logger;

    /// <summary>
    /// Initializes a new instance of the transaction report controller.
    /// </summary>
    public ReportTransactionController(
        UserManager<AppUser> userManager,
        AppDbContext context,
        ILogger<ReportTransactionController> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Displays the main page for financial movement history.
    /// </summary>
    /// <returns>
    /// The Index View populated with the logged-in user's transaction list.
    /// Redirects to a global error page if the database query fails.
    /// </returns>
    public async Task<IActionResult> Index()
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao carregar o histórico de transações.");

            return RedirectToAction("Error", "Home", new
            {
                area = "",
                errorCode = (int)AppErrors.DatabaseQueryError
            });
        }
    }

    /// <summary>
    /// Filters the transaction history based on search criteria, type, and date.
    /// </summary>
    /// <param name="searchString">Search term for transaction description or reference.</param>
    /// <param name="typeFilter">Filter for "In" (positive) or "Out" (negative) movements.</param>
    /// <param name="dateFilter">Minimum date for inclusion in the report.</param>
    /// <returns>A PartialView containing the filtered table rows, or 500 status on error.</returns>
    [HttpGet]
    public async Task<IActionResult> GetFilteredBalance(string? searchString, string? typeFilter, DateTime? dateFilter)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var query = _context.Transaction
                .Include(t => t.User)
                .Where(t => t.User.Id == user.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var search = searchString.ToLower();
                query = query.Where(t => t.Description!.ToLower().Contains(search) ||
                                     t.Reference.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(typeFilter))
            {
                if (typeFilter == "Entrada") query = query.Where(t => t.Amount > 0);
                else if (typeFilter == "Saida") query = query.Where(t => t.Amount < 0);
            }

            if (dateFilter.HasValue)
            {
                query = query.Where(t => t.CreatedAt.Date >= dateFilter.Value.Date);
            }

            var result = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            return PartialView("_BalanceHistoryRows", result);
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.DatabaseQueryError, TableName.Transaction, AppOperation.Read, ex);

            var msg = $"{Errors.DatabaseQueryError} [Erro: {(int)AppErrors.DatabaseQueryError}]";
            return StatusCode(500, new { failMessage = msg });
        }
    }
}
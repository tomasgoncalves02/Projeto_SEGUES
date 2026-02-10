using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Report;

[Authorize]
[Area("Report")]
public class ReportController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    
    /*
    [Authorize]
    public async Task<IActionResult> HistoricoSenhas(string searchString, TicketState? stateFilter, string flowFilter)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Query base: carregamos as transferências e os envolvidos
        var query = _context.Tickets
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .Include(t => t.Transfers).ThenInclude(tr => tr.Sender)
            .Include(t => t.Transfers).ThenInclude(tr => tr.Receiver)
            .Where(t => t.Owner.Id == user.Id || t.Transfers.Any(tr => tr.Sender.Id == user.Id || tr.Receiver.Id == user.Id))
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(t =>
                // 1. Pesquisa no Código da Senha
                t.ValidationCode.Contains(searchString.ToUpper()) ||

                // 2. Pesquisa nas Transferências (Quem recebeu ou quem enviou)
                t.Transfers.Any(tr =>
                    tr.Receiver.FirstName.Contains(searchString) ||
                    tr.Receiver.LastName.Contains(searchString) ||
                    tr.Sender.FirstName.Contains(searchString) ||
                    tr.Sender.LastName.Contains(searchString)                     
                )
            );
        }

        // 2. Filtro por Estado
        if (stateFilter.HasValue)
            query = query.Where(t => t.State == stateFilter.Value);

        // 3. Filtro por Fluxo (Compradas, Enviadas, Recebidas)
        if (!string.IsNullOrEmpty(flowFilter))
        {
            switch (flowFilter)
            {
                case "Compradas":
                    query = query.Where(t => t.Owner.Id == user.Id && !t.Transfers.Any(tr => tr.Receiver.Id == user.Id));
                    break;
                case "Enviadas":
                    query = query.Where(t => t.Transfers.Any(tr => tr.Sender.Id == user.Id));
                    break;
                case "Recebidas":
                    query = query.Where(t => t.Transfers.Any(tr => tr.Receiver.Id == user.Id));
                    break;
            }
        }

        ViewData["CurrentSearch"] = searchString;
        ViewData["CurrentState"] = stateFilter;
        ViewData["CurrentFlow"] = flowFilter;
        ViewBag.CurrentUserId = user.Id;

        var tickets = await query.OrderByDescending(t => t.TicketPurchase.TransactionDate).ToListAsync();
        return View(tickets);
    }*/
}
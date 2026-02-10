using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Admin;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
    
    /*
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePrices(List<TicketPrice> updatedPrices)
    {
        var today = DateTime.Now.Date;

        // 1. Validação de Segurança: Verificar se alguma data é anterior a hoje
        if (updatedPrices.Any(p => p.EndDatePrice.Date < today))
        {
            TempData["Error"] = "Erro: A data de validade não pode ser inferior à data de hoje.";
            return RedirectToAction(nameof(GestaoSenhas));
        }

        if (ModelState.IsValid)
        {
            try
            {
                foreach (var price in updatedPrices)
                {
                    // Garante que a data gravada seja o final do dia (23:59:59) 
                    // para que a senha não expire logo ao início do dia escolhido
                    price.EndDatePrice = price.EndDatePrice.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                    _context.TicketPrices.Update(price);
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "O preçário e as datas foram atualizados com sucesso!";
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocorreu um erro ao gravar os novos preços na base de dados.";
            }
        }
        else
        {
            TempData["Error"] = "Os dados introduzidos são inválidos.";
        }

        return RedirectToAction(nameof(GestaoSenhas));
    }
    
    
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GestaoSenhas()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
            
        var allPrices = await _context.TicketPrices
            .Include(p => p.UserCategory)
            .ToListAsync();
        ViewBag.Prices = allPrices;

        // 2. Auditoria Global: Carrega tickets de TODOS os utilizadores
        var allTickets = await _context.Tickets
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .OrderByDescending(t => t.TicketPurchase.TransactionDate)
            .ToListAsync();

        return View(allTickets);
    }*/
}
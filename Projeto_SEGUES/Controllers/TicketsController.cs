using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.ViewModels;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public TicketsController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Refeitorio()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ViewBag.UserBalance = user.Balance;
            return View();
        }

        public IActionResult HistoricoMenu()
        {
            return View();
        }


        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var now = DateTime.Now;

            // 1. Expira apenas os tickets DO UTILIZADOR logado (para performance)
            var expiredTickets = await _context.Tickets
                .Where(t => t.Owner.Id == user.Id && t.State == TicketState.Available && t.ExpirationDate < now)
                .ToListAsync();

            if (expiredTickets.Any())
            {
                foreach (var ticket in expiredTickets) { ticket.State = TicketState.Expired; }
                await _context.SaveChangesAsync();
            }

            // 2. Dados da Loja (Sempre baseados no perfil de quem está logado)
            decimal currentPrice = await GetCurrentPriceFromDb(user.UserCategory);

            ViewBag.UserBalance = user.Balance;
            ViewBag.CurrentPrice = currentPrice;
            
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault();

            // 3. Consulta APENAS os tickets do próprio utilizador
            // Removemos o "canSeeAll" aqui para que o Admin veja apenas as suas senhas
            var myTickets = await _context.Tickets
                .Include(t => t.Owner)
                .Include(t => t.TicketPurchase)
                .Where(t => t.Owner.Id == user.Id) // Filtro obrigatório para todos
                .OrderByDescending(t => t.TicketPurchase.TransactionDate)
                .ToListAsync();

            return View(myTickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyTicket(int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User); 
            if (user == null) return Challenge();
            
            await _context.Entry(user).Reference(u => u.UserCategory).LoadAsync();

            var now = DateTime.Now;
            decimal pricePerUnit = await GetCurrentPriceFromDb(user.UserCategory);

            var priceConfig = await _context.TicketPrices
                .Where(p => p.UserCategory.Id == user.UserCategory.Id && now >= p.InitialDatePrice && now <= p.EndDatePrice)
                .OrderByDescending(p => p.InitialDatePrice)
                .FirstOrDefaultAsync();
            DateTime dataExpiracao = priceConfig?.EndDatePrice ?? now.AddDays(30);

            if (pricePerUnit <= 0)
            {
                TempData["Error"] = "Preçário não disponível. Contacte a administração.";
                return RedirectToAction(nameof(Index));
            }

            decimal totalCost = pricePerUnit * quantity;
            if (user.Balance < totalCost)
            {
                TempData["Error"] = "Saldo insuficiente para a operação.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var purchase = new TicketPurchase
                {
                    User = user,
                    Quantity = quantity,
                    TransactionDate = now,
                    Value = totalCost
                };
                _context.TicketPurchases.Add(purchase);
                await _context.SaveChangesAsync();

                for (int i = 0; i < quantity; i++)
                {
                    _context.Tickets.Add(new Ticket
                    {
                        Owner = user,
                        ExpirationDate = dataExpiracao,
                        State = TicketState.Available,
                        TicketPurchase = purchase,
                        ValidationCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                    });
                }

                user.Balance -= totalCost;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Compra realizada! Verifique as suas senhas abaixo.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Erro ao processar a compra.";
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ValidateTicket()
        {
            var model = new ValidateTicketViewModel
            {
                RecentTickets = await GetRecentTicketsAsync()
            };
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ValidateTicket(ValidateTicketViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RecentTickets = await GetRecentTicketsAsync();
                return View(model);
            }


            var ticket = await _context.Tickets
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.ValidationCode == model.Code.ToUpper());


            if (ticket == null)
            {
                ModelState.AddModelError("Code", "Código não encontrado.");
            }
            else if (ticket.State == TicketState.Used)
            {
                ModelState.AddModelError("Code", $"AVISO: Senha já utilizada em {ticket.UsedDate:dd/MM HH:mm}.");
            }
            else if (ticket.State == TicketState.Expired || ticket.ExpirationDate < DateTime.Now)
            {
                if (ticket.State != TicketState.Expired)
                {
                    ticket.State = TicketState.Expired;
                    await _context.SaveChangesAsync();
                }
                ModelState.AddModelError("Code", "ERRO: A senha expirou.");
            }
            else
            {

                ticket.State = TicketState.Used;
                ticket.UsedDate = DateTime.Now;
                ticket.IsUsed = true;

                _context.Tickets.Update(ticket);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Senha validada!";

                // Limpar campo
                ModelState.Clear();
                model.Code = string.Empty;
            }


            model.RecentTickets = await GetRecentTicketsAsync();
            return View(model);
        }
        
        // TODO: apagar?
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult OperacaoRefeitorio()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> SenhasAtivas()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var now = DateTime.Now;

            // Filtra apenas senhas Disponíveis e que ainda não expiraram
            var activeTickets = await _context.Tickets
                .Include(t => t.TicketPurchase)
                .Where(t => t.Owner.Id == user.Id &&
                            t.State == TicketState.Available &&
                            t.ExpirationDate >= now)
                .OrderBy(t => t.ExpirationDate) // Mostra as que expiram mais cedo primeiro
                .ToListAsync();
            
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault();
            return View(activeTickets);
        }

        private async Task<List<Ticket>> GetRecentTicketsAsync()
        {
            return await _context.Tickets
                .Include(t => t.Owner)
                .Where(t => t.State == TicketState.Used)
                .OrderByDescending(t => t.UsedDate)
                .Take(10)
                .ToListAsync();
        }

        

        private async Task<decimal> GetCurrentPriceFromDb(UserCategory category)
        {
            var now = DateTime.Now;
            var priceEntry = await _context.TicketPrices
                .Where(p => p.UserCategory == category && now >= p.InitialDatePrice && now <= p.EndDatePrice)
                .OrderByDescending(p => p.InitialDatePrice)
                .FirstOrDefaultAsync();

            return priceEntry?.Price ?? 0m;
        }

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
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models;
using static Projeto_SEGUES.Models.Enums.Enums;

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

        // RF17: Vista principal para ver saldo e comprar
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var now = DateTime.Now;
            // STAFF e ADMIN podem ver todas as senhas (Auditoria/Consulta)
            bool canSeeAll = user.Role == UserRole.Admin || user.Role == UserRole.Employee;

            // 1. RF16: AUTO-EXPIRAÇÃO (Limpeza ao carregar)
            var expiredQuery = _context.Tickets
                .Where(t => t.State == TicketState.Available && t.ExpirationDate < now);

            if (!canSeeAll)
            {
                expiredQuery = expiredQuery.Where(t => t.OwnerId == user.Id);
            }

            var expiredTickets = await expiredQuery.ToListAsync();
            if (expiredTickets.Any())
            {
                foreach (var ticket in expiredTickets) { ticket.State = TicketState.Expired; }
                await _context.SaveChangesAsync();
            }
            if (user.Role == UserRole.Admin)
            {
                var allPrices = await _context.TicketPrices.ToListAsync();

                // Se a tabela estiver vazia (primeira vez), criamos os registos base
                if (!allPrices.Any())
                {
                    allPrices = new List<TicketPrice>
            {
                new TicketPrice { TicketType = TicketType.Student, Price = 2.90m, InitialDatePrice = now, EndDatePrice = now.AddYears(1) },
                new TicketPrice { TicketType = TicketType.DocenteNaoDocente, Price = 5.20m, InitialDatePrice = now, EndDatePrice = now.AddYears(1) },
                new TicketPrice { TicketType = TicketType.External, Price = 5.50m, InitialDatePrice = now, EndDatePrice = now.AddYears(1) }
            };
                    _context.TicketPrices.AddRange(allPrices);
                    await _context.SaveChangesAsync();
                }
                ViewBag.Prices = allPrices;
            }

            // 2. RF17: PREÇO DINÂMICO (Baseado no Admin)
            var userTicketType = GetTicketTypeByUserRole(user.Role);
            decimal currentPrice = await GetCurrentPriceFromDb(userTicketType);

            ViewBag.UserBalance = user.Balance;
            ViewBag.CurrentPrice = currentPrice;
            ViewBag.UserRole = user.Role;

            // 3. LISTAGEM (Admin e Staff vêm tudo)
            IQueryable<Ticket> ticketsQuery = _context.Tickets.Include(t => t.Owner);
            if (!canSeeAll)
            {
                ticketsQuery = ticketsQuery.Where(t => t.OwnerId == user.Id);
            }

            var ticketsToShow = await ticketsQuery.OrderByDescending(t => t.PurchaseDate).ToListAsync();
            return View(ticketsToShow);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> BuyTicket(int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);

            // Bloqueio: Apenas Estudante, DocenteNaoDocente e Externo compram
            if (user.Role == UserRole.Admin || user.Role == UserRole.Employee)
            {
                TempData["Error"] = "Este perfil não tem permissão para comprar senhas.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;
            var ticketType = GetTicketTypeByUserRole(user.Role);
            decimal pricePerUnit = await GetCurrentPriceFromDb(ticketType);

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
                    UserId = user.Id,
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
                        OwnerId = user.Id,
                        PurchaseDate = now,
                        ExpirationDate = now.AddDays(7), // RF16: Validade
                        State = TicketState.Available,
                        TicketPurchaseId = purchase.Id,
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

        // --- IMPLEMENTAÇÃO RF16 (IMPEDIR REUTILIZAÇÃO/EXPIRAÇÃO) ---
        // Este método seria chamado pelo Funcionário ao validar a senha
        [Authorize(Roles = "Staff,Admin")]
        [HttpPost]
        public async Task<IActionResult> ValidateTicket(string code)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.ValidationCode == code);

            if (ticket == null)
                return Json(new { success = false, message = "Código inválido." });

            if (ticket.State != TicketState.Available)
                return Json(new { success = false, message = "Esta senha já foi usada ou expirou." });

            if (ticket.ExpirationDate < DateTime.Now)
            {
                ticket.State = TicketState.Expired;
                await _context.SaveChangesAsync();
                return Json(new { success = false, message = "Esta senha expirou." });
            }

            // Validar com sucesso
            ticket.State = TicketState.Used;
            ticket.UsedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Validado! Bom apetite." });
        }

        // Método auxiliar para ir buscar o preço real à BD
        private TicketType GetTicketTypeByUserRole(UserRole role)
        {
            return role switch
            {
                UserRole.Student => TicketType.Student,
                UserRole.DocenteNaoDocente => TicketType.DocenteNaoDocente,
                UserRole.External => TicketType.External,
                UserRole.Employee => TicketType.Employee, // Staff/Funcionário
                UserRole.Admin => TicketType.Admin,
                _ => TicketType.External
            };
        }

        private async Task<decimal> GetCurrentPriceFromDb(TicketType userType)
        {
            var now = DateTime.Now;
            var priceEntry = await _context.TicketPrices
                .Where(p => p.TicketType == userType && now >= p.InitialDatePrice && now <= p.EndDatePrice)
                .OrderByDescending(p => p.InitialDatePrice)
                .FirstOrDefaultAsync();

            return priceEntry?.Price ?? 0m;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePrices(List<TicketPrice> updatedPrices)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    foreach (var price in updatedPrices)
                    {
                        // Parte 3: Operadores do domínio sendo atualizados
                        _context.TicketPrices.Update(price);
                    }
                    await _context.SaveChangesAsync();

                    // Esta mensagem será lida pelo SweetAlert no Scripts da View
                    TempData["Success"] = "O precario foi atualizado com sucesso!";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Ocorreu um erro ao gravar os novos precos na base de dados.";
                }
            }
            else
            {
                TempData["Error"] = "Os dados introduzidos são invalidos.";
            }

            // CORREÇÃO DA ROTA: Força o retorno para a página de Gestão de Senhas
            return RedirectToAction("Index", "Tickets");
        }
    }


    }
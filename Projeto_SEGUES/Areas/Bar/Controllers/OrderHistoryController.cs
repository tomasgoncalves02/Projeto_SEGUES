using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Necessário para o ToListAsync
using Projeto_SEGUES.Areas.Bar.ViewModels;
using Projeto_SEGUES.Data; // Ajusta para o namespace onde está o teu DbContext
using System.Security.Claims; // Necessário para ir buscar o ID do utilizador

namespace Projeto_SEGUES.Areas.Bar.Controllers
{
    [Area("Bar")]
    public class OrderHistoryController : Controller
    {
        private readonly AppDbContext _context; // AJUSTA: Substitui pelo nome do teu DbContext

        public OrderHistoryController(AppDbContext context) // AJUSTA: Substitui pelo nome do teu DbContext
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Ir buscar o ID do utilizador que está logado
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // 2. Ir buscar as encomendas reais da Base de Dados para este utilizador
            var ordersFromDb = await _context.BarOrders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // 3. Mapear para o ViewModel (se a lista estiver vazia, o select devolve uma lista vazia)
            var model = ordersFromDb.Select(o => new OrderHistoryViewModel
            {
                Codigo = o.RedemptionCode,
                DataCompra = o.OrderDate,
                HoraRecolha = o.OrderPickUp,
                StatusValue = o.Status,
                Validade = o.Expired,
                Recolhido = o.PickDate,
                PrecoTotal = o.PriceAtTime,

              
                Estado = o.Status switch
                {
                    0 => "Pendente",
                    1 => "Em preparação",
                    2 => "Entrega Pendente",
                    3 => "Entregue",
                    _ => "Cancelado"
                }
            }).ToList();

            return View(model);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Bar;

namespace Projeto_SEGUES.Areas.Bar.Controllers
{
    [Area("Bar")]
    [Authorize(Roles = "Admin")]
    public class OrderManagementController : Controller
    {
        private readonly AppDbContext _context;

        public OrderManagementController(AppDbContext context)
        {
            _context = context;
        }

        // Dashboard com os cards (Gestão de Produtos / Histórico)
        public IActionResult Index()
        {
            return View();
        }

        // Listagem de todos os pedidos efetuados
        public async Task<IActionResult> AllOrders()
        {
            var history = await _context.BarOrders
                .Include(o => o.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(history);
        }
    }
}
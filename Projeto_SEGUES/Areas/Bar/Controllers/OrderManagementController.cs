using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Bar.ViewModels;
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

        [Area("Bar")]
        public async Task<IActionResult> CreateOrder()
        {
            // Vamos buscar os produtos que têm stock disponível na Gestão de Inventário
            var products = await _context.Products
                .Where(p => p.Stock > 0)
                .Select(p => new ProductItemViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock
                }).ToListAsync();

            var model = new PlaceOrderViewModel { AvailableProducts = products };
            return View(model);
        }
    }
}
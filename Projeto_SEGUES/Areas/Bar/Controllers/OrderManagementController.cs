using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Projeto_SEGUES.Areas.Bar.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Bar;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Payment;

namespace Projeto_SEGUES.Areas.Bar.Controllers
{
    [Area("Bar")]
    [Authorize]
    public class OrderManagementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public OrderManagementController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index() => View();

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllOrders()
        {
            // Carregamos o pedido, o produto relacionado e o utilizador que fez o pedido
            var history = await _context.BarOrders
                .Include(o => o.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(history);
        }        
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var baseOrder = await _context.BarOrders.FindAsync(id);
            if (baseOrder == null) return NotFound();

            var produtos = await _context.BarOrders
                .Include(o => o.Product)
                .Where(o => o.RedemptionCode == baseOrder.RedemptionCode)
                .Select(o => new
                {
                    nome = o.Product.Name,
                    preco = o.PriceAtTime,
                    quantidade = o.Quantity // Lê a nova coluna física
                })
                .ToListAsync();

            return Json(new
            {
                codigo = baseOrder.RedemptionCode,
                produtos = produtos
            });
        }

    }

}
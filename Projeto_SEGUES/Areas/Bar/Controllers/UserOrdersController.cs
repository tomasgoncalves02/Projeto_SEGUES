using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data; // Ajusta para o teu namespace de Data
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Bar.Controllers
{
    [Area("Bar")]
    public class UserOrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public UserOrdersController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Página Principal: Os Meus Pedidos
        public async Task<IActionResult> ActiveOrders()
        {
            var userId = _userManager.GetUserId(User);

            var orders = await _context.BarOrders
                .Include(o => o.Product)
                .Where(o => o.UserId == userId && o.Status < 3) // Apenas estados 0, 1 e 2
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Endpoint para o HTMX atualizar os cartões
        [HttpGet]
        public async Task<IActionResult> GetUpdatedActiveOrders()
        {
            var userId = _userManager.GetUserId(User);

            var orders = await _context.BarOrders
                .Include(o => o.Product)
                .Where(o => o.UserId == userId && o.Status < 3)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return PartialView("_ActiveOrdersCards", orders);
        }
    }
}
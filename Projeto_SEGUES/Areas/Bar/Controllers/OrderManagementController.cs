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

        public async Task<IActionResult> ManageOrders()
        {
            var orders = await _context.BarOrders
                .Include(o => o.Product)
                .Include(o => o.User)
                .Where(o => o.Status < 3) 
                .OrderBy(o => o.OrderPickUp == TimeSpan.Zero ? o.OrderDate.TimeOfDay : o.OrderPickUp)
                .ToListAsync();

            return View(orders);
        }

        //Apenas a tabela 
        [HttpGet]
        public async Task<IActionResult> GetOrdersTable()
        {
            var orders = await _context.BarOrders
                .Include(o => o.Product)
                .Include(o => o.User)
                .Where(o => o.Status < 3)
                .OrderBy(o => o.OrderPickUp == TimeSpan.Zero ? o.OrderDate.TimeOfDay : o.OrderPickUp)
                .ToListAsync();

            return PartialView("_ManageOrdersTable", orders);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int newStatus)
        {
            var order = await _context.BarOrders.FindAsync(id);
            if (order == null) return NotFound();

            // Atualiza todos os itens que têm o mesmo código de redenção
            var relatedOrders = await _context.BarOrders
                .Where(o => o.RedemptionCode == order.RedemptionCode)
                .ToListAsync();

            foreach (var o in relatedOrders)
            {
                o.Status = newStatus;
                // Se por acaso alguém puser entregue por aqui, marcamos como consumido
                if (newStatus == 3) o.IsConsumed = true;
            }

            await _context.SaveChangesAsync();
            return Ok(); // Retorna OK para o JavaScript saber que deu certo
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

        [HttpGet]
        public async Task<IActionResult> GetOrderDetailsSide(int id)
        {
            var order = await _context.BarOrders
                .Include(o => o.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Agrupa itens do mesmo pedido pelo código de redenção
            var itemsInOrder = await _context.BarOrders
                .Include(o => o.Product)
                .Where(o => o.RedemptionCode == order.RedemptionCode)
                .ToListAsync();

            ViewBag.ItemsInOrder = itemsInOrder;
            return PartialView("_OrderDetailsSideCard", order);
        }

        [HttpPost]
        public async Task<IActionResult> ValidateOrderCode(int id, string codeEntered)
        {
            var order = await _context.BarOrders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.RedemptionCode.Trim().ToUpper() == codeEntered?.Trim().ToUpper())
            {
                var relatedOrders = await _context.BarOrders
                    .Where(o => o.RedemptionCode == order.RedemptionCode).ToListAsync();

                foreach (var o in relatedOrders)
                {
                    o.Status = 3; // Entregue
                    o.IsConsumed = true;
                    o.PickDate = DateTime.Now; // REGISTA A HORA EXATA AQUI
                }

                await _context.SaveChangesAsync();
                Response.Headers.Add("HX-Trigger", "orderUpdated");
                return Ok(new { success = true });
            }
            return BadRequest(new { message = "Código inválido!" });
        }

    }

}
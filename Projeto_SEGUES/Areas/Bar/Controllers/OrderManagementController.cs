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

        // Loja: Listagem de Produtos
        public async Task<IActionResult> CreateOrder()
        {
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

            return View(new PlaceOrderViewModel { AvailableProducts = products });
        }

        // Carrinho: Checkout Dinâmico (Lê da BD)
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var dbItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            var model = new CartViewModel
            {
                UserBalance = user.Balance,
                TotalAmount = dbItems.Sum(i => i.Product.Price * i.Quantity),
                Items = dbItems.Select(i => new CartItemViewModel
                {
                    ProductId = i.ProductId,
                    Name = i.Product.Name,
                    Price = i.Product.Price,
                    Quantity = i.Quantity,
                    Description = i.Product.Description
                }).ToList()
            };

            // Passamos o JSON para a View carregar no elemento #swal-data se houver mensagens
            ViewData["SwalJson"] = TempData["SwalJson"];

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, int qty)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == id);

            if (existingItem != null) existingItem.Quantity += qty;
            else _context.CartItems.Add(new CartItem { UserId = userId, ProductId = id, Quantity = qty });

            await _context.SaveChangesAsync();
            var totalCount = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new { success = true, count = totalCount });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == id);
            if (item == null) return Json(new { success = false });

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var baseOrder = await _context.BarOrders.FindAsync(id);
            if (baseOrder == null) return NotFound();

            var produtos = await _context.BarOrders
                .Include(o => o.Product)
                .Where(o => o.RedemptionCode == baseOrder.RedemptionCode)
                .Select(o => new {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPurchase(bool receiveNow)
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction(nameof(CreateOrder));

            decimal total = cartItems.Sum(i => i.Product.Price * i.Quantity);

            // Validação de Saldo
            if (user.Balance < total)
            {
                TempData["SwalJson"] = JsonConvert.SerializeObject(new
                {
                    icon = "error",
                    title = "Saldo Insuficiente",
                    text = $"Total: {total:N2}€"
                });
                return RedirectToAction(nameof(Checkout));
            }

            // Processamento da compra
            user.Balance -= total;
            foreach (var item in cartItems)
            {
                var dbP = await _context.Products.FindAsync(item.ProductId);
                if (dbP != null) dbP.Stock -= item.Quantity;

                _context.BarOrders.Add(new BarOrder
                {
                    UserId = user.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    OrderDate = DateTime.Now,
                    PriceAtTime = item.Product.Price,
                    Status = 0, // Pendente
                    RedemptionCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                });
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // Mensagem de SUCESSO para o site.js ler após o redirecionamento
            TempData["SwalJson"] = JsonConvert.SerializeObject(new
            {
                icon = "success",
                title = "Compra efetuada com sucesso!",
                text = "O seu pedido já se encontra pendente no bar."
            });

            // Manda para a página do histórico do bar
            return RedirectToAction("Index", "OrderHistory", new { area = "Bar" });
        }
    }

}
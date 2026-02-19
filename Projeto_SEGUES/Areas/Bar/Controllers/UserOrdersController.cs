using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Projeto_SEGUES.Areas.Bar.ViewModels;
using Projeto_SEGUES.Data; // Ajusta para o teu namespace de Data
using Projeto_SEGUES.Models.Bar;
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

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            // 1. Primeiro buscamos o pedido pelo ID para saber qual é o RedemptionCode
            var pedidoReferencia = await _context.BarOrders.FindAsync(id);
            if (pedidoReferencia == null) return NotFound();

            // 2. Buscamos todos os produtos que pertencem a esse mesmo código de grupo
            var todosOsProdutos = await _context.BarOrders
                .Include(o => o.Product) // Certifica-te que incluis a tabela de produtos
                .Where(o => o.RedemptionCode == pedidoReferencia.RedemptionCode)
                .ToListAsync();

            // 3. Montamos o objeto JSON exatamente como o teu site.js espera
            return Json(new
            {
                codigo = pedidoReferencia.RedemptionCode,
                produtos = todosOsProdutos.Select(p => new {
                    nome = p.Product.Name,
                    preco = p.PriceAtTime,
                    quantidade = p.Quantity
                })
            });
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPurchase(bool receiveNow, string? pickupTime)
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction(nameof(CreateOrder));

            decimal total = cartItems.Sum(i => i.Product.Price * i.Quantity);

            // 1. Validação de Saldo
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

            // 2. GERAR CÓDIGO ÚNICO PARA TODO O PEDIDO
            // Geramos o código aqui fora para que todos os produtos tenham o mesmo
            string codigoUnicoPedido = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();


            TimeSpan orderPickUp = receiveNow
            ? DateTime.Now.TimeOfDay
            : TimeSpan.Parse(pickupTime!);


            // 3. Processamento da compra
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
                    OrderPickUp = orderPickUp,
                    CreationTime = DateOnly.FromDateTime(DateTime.Today),
                    Expired = DateOnly.FromDateTime(DateTime.Today), 
                    PriceAtTime = item.Product.Price,
                    Status = 0,
                    RedemptionCode = codigoUnicoPedido
                });
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // 4. Mensagem de SUCESSO
            TempData["SwalJson"] = JsonConvert.SerializeObject(new
            {
                icon = "success",
                title = "Compra efetuada com sucesso!",
                text = "O seu pedido já se encontra pendente no bar."
            });

            // 5. REDIRECIONAMENTO
            // Como agora tens a página de cartões (ActiveOrders), deves mandar o user para lá
            return RedirectToAction("ActiveOrders", "UserOrders", new { area = "Bar" });
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
    }
}
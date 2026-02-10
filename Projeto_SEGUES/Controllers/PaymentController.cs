using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Purchase;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Projeto_SEGUES.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public PaymentController(AppDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public IActionResult Deposit() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateMbWay(decimal amount, string phoneNumber)
        {
            if (amount <= 0 || string.IsNullOrEmpty(phoneNumber))
                return BadRequest("Invalid input data.");

            // Obtém o ID do utilizador logado no ASP.NET Identity
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge(); // Redireciona para login se não estiver logado

            // 1. Criar e Salvar Transação
            var transaction = new Transaction
            {
                UserId = userId,
                Amount = amount,
                PhoneNumber = phoneNumber,
                Reference = Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
            };

            _context.Set<Transaction>().Add(transaction);
            await _context.SaveChangesAsync();

            // 2. Chamada à API (Simulação de Sandbox)
            // Nota: Se não tiveres o HttpClient configurado no Program.cs, isto vai dar erro de runtime.
            try
            {
                var client = _clientFactory.CreateClient("MbWayClient");
                var response = await client.PostAsJsonAsync("api/mbway/pay", new
                {
                    amount = transaction.Amount,
                    phone = transaction.PhoneNumber,
                    reference = transaction.Reference,
                    sandbox = true
                });
            }
            catch (Exception)
            {
                // Em ambiente de teste local, ignoramos erros de rede se o endpoint não existir
            }

            return View("Waiting", transaction);
        }

        [HttpGet]
        [Route("api/payment/callback")]
        public async Task<IActionResult> Callback(string reference, string status)
        {
            // 1. Procura a transação (Lógica que já confirmaste que funciona)
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Reference == reference && !t.IsPaid);

            if (transaction != null && status == "success")
            {
                // 2. Procura o utilizador e atualiza o saldo
                var user = await _context.Users.FindAsync(transaction.UserId);
                if (user != null)
                {
                    user.Balance += transaction.Amount; // ATENÇÃO: Garante que esta linha não tem //
                    transaction.IsPaid = true;

                    // Força a atualização do utilizador no contexto
                    _context.Users.Update(user);
                }

                // 3. Guarda todas as alterações (Saldo + Estado da Transação)
                await _context.SaveChangesAsync();

                // 4. Prepara o Popup para a Home
                TempData["PaymentStatus"] = "Success";
                TempData["PaymentMessage"] = $"Sucesso! Foram carregados {transaction.Amount:C2} na sua conta.";

                return RedirectToAction("Index", "Home");
            }

            // Se falhar
            TempData["PaymentStatus"] = "Error";
            TempData["PaymentMessage"] = "Não foi possível processar o pagamento.";
            return RedirectToAction("Index", "Home");
        }
    }
}
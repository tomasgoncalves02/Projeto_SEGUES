using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Payment
{
    [Area("Payment")]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly UserManager<AppUser> _userManager;

        public PaymentController(AppDbContext context, IHttpClientFactory clientFactory, UserManager<AppUser> userManager)
        {
            _context = context;
            _clientFactory = clientFactory;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Deposit() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateMbWay(decimal amount, string phoneNumber)
        {
            if (amount <= 0 || string.IsNullOrEmpty(phoneNumber))
                return BadRequest("Dados inválidos.");
            
            // Get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge(); // Return to login if user is not authenticated

            // Create and save the transaction
            var transaction = new Transaction
            {
                User = user,
                Amount = amount,
                PhoneNumber = phoneNumber,
                Reference = Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
            };

            _context.Set<Transaction>().Add(transaction);
            await _context.SaveChangesAsync();

            // Call to Sandbox MBWay API. Requires HttpClient in Program.cs.
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
                // In test mode, ignore errors
            }

            return View("Waiting", transaction);
        }

        [HttpGet]
        [Route("api/payment/callback")]
        public async Task<IActionResult> Callback(string reference, string status)
        {
            // Get transaction by reference
            var transaction = await _context.Transaction
                .Include(t => t.User) // Include user
                .FirstOrDefaultAsync(t => t.Reference == reference && !t.IsPaid);

            if (transaction != null && status == "success")
            {
                // Update user balance
                var user = transaction.User;
                user.Balance += transaction.Amount;
                transaction.IsPaid = true;

                // Update user and save
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Set swal
                TempData.SetSwalSuccess($"Foram carregados {transaction.Amount:C2} na sua conta.");
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            // If fail
            TempData.SetSwalError("Não foi possível processar o pagamento. Por favor, tente novamente.");
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
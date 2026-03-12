using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.User;
using Stripe;
using Stripe.Checkout;


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
        public IActionResult Deposit()
        {
            
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(decimal amount)
        {
            if (amount <= 0)
                return BadRequest("Dados inválidos.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge(); // Return to login if user is not authenticated

            var currency = "eur";

            var transaction = new Transaction
            {
                User = user,
                Amount = Convert.ToDecimal(amount),
                Reference = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                IsPaid = false 
            };
           

            _context.Set<Transaction>().Add(transaction);
            await _context.SaveChangesAsync();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>{"card"},
                LineItems = new List<SessionLineItemOptions>
                {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = (long)(Convert.ToDecimal(amount) * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Carregar Saldo",
                            Description = $"Carregamento de saldo no valor de {amount:C2} para a conta SEGUES."
                        }
                    },
                    Quantity = 1
                }
                },
                Mode = "payment",
                SuccessUrl = Url.Action("SuccessPayment", "Payment", new { reference = transaction.Reference }, Request.Scheme),
                CancelUrl = Url.Action("CancelPayment", "Payment", null, Request.Scheme)
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }

        [HttpGet]
        public async Task<IActionResult> SuccessPayment(string reference)
        {
            var transaction = await _context.Transaction
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Reference == reference && !t.IsPaid);

            if (transaction != null)
            {
                var user = transaction.User;
                user.Balance += transaction.Amount;
                transaction.IsPaid = true;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData.SetSwalSuccess($"Foram carregados {transaction.Amount:C2} com sucesso!");


                return RedirectToAction("Index", "Home", new { area = "" });
            }

            TempData.SetSwalError("Pagamento não encontrado ou já processado.");
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet]
        public IActionResult CancelPayment()
        {

            TempData.SetSwalError("Pagamento cancelado.");
            return RedirectToAction("Index", "Home", new { area = "" });
        }




    }
}
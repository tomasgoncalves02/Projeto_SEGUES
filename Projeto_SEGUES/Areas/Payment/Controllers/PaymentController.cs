using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Stripe.Checkout;

namespace Projeto_SEGUES.Areas.Payment
{
    /// <summary>
    /// Controller responsible for managing payments and balance top-ups via Stripe.
    /// </summary>
    /// <remarks>
    /// This controller handles checkout session creation, success confirmation processing, 
    /// and management of pending transactions in the database.
    /// </remarks>
    [Area("Payment")]
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<PaymentController> _logger;
        private readonly IStringLocalizer<Errors> _localizer;

        /// <summary>
        /// Initializes a new instance of the payment controller with database context, identity, logging, and localization.
        /// </summary>
        public PaymentController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<PaymentController> logger,
            IStringLocalizer<Errors> localizer)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _localizer = localizer;
        }

        /// <summary>
        /// Displays the initial page for choosing the top-up amount.
        /// </summary>
        /// <returns>The Deposit View.</returns>
        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        /// <summary>
        /// Creates a Stripe Checkout session and records a pending transaction in the system.
        /// </summary>
        /// <param name="amount">Monetary value to charge to the account.</param>
        /// <returns>Redirect to Stripe's secure payment form or an error page on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(decimal amount)
        {
            if (amount <= 0)
            {
                TempData.SetSwalError("O valor de carregamento deve ser superior a zero.");
                return RedirectToAction(nameof(Deposit));
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                var transaction = new Transaction
                {
                    User = user,
                    Amount = amount,
                    Reference = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    IsPaid = false,
                    CreatedAt = DateTime.Now // Assumindo que tens este campo para auditoria
                };

                _context.Set<Transaction>().Add(transaction);
                await _context.SaveChangesAsync();

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "eur", 
                                UnitAmount = (long)(amount * 100),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Carregar Saldo - SEGUES",
                                    Description = $"Carregamento de conta no valor de {amount:C2}"
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
                var session = await service.CreateAsync(options);

                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                _logger.LogAppError($"Erro ao criar sessão Stripe: {ex.Message}", TableName.Payment, AppOperation.Create);

                var erroEnum = AppErrors.InternalServerError;
                var msg = $"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]";

                TempData.SetSwalError(msg);
                return RedirectToAction(nameof(Deposit));
            }
        }

        /// <summary>
        /// Processes the success confirmation from Stripe and updates user balance.
        /// </summary>
        /// <param name="reference">Internal transaction reference generated at the start.</param>
        /// <returns>Redirect to Home with success notification or error message.</returns>
        [HttpGet]
        public async Task<IActionResult> SuccessPayment(string reference)
        {
            try
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

                TempData.SetSwalError("Transação não encontrada ou já processada.");
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            catch (Exception ex)
            {
                _logger.LogAppError($"Erro crítico ao confirmar pagamento {reference}: {ex.Message}", TableName.Payment, AppOperation.Update);

                var erroEnum = AppErrors.DatabaseUpdateError;
                var msg = $"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]";

                TempData.SetSwalError(msg);
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        /// <summary>
        /// Handles the return when a payment is canceled or interrupted.
        /// </summary>
        [HttpGet]
        public IActionResult CancelPayment()
        {
            TempData.SetSwalInfo("O processo de pagamento foi cancelado pelo utilizador.");
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
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
    /// <summary>
    /// Controller responsável por gerir os pagamentos e carregamentos de saldo via Stripe.
    /// </summary>
    /// <remarks>
    /// Este controlador lida com a criação de sessões de checkout, processamento de confirmações 
    /// de sucesso e gestão de transações pendentes na base de dados.
    /// </remarks>
    [Area("Payment")]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly UserManager<AppUser> _userManager;

        /// <summary>
        /// Inicializa uma nova instância do controlador de pagamentos.
        /// </summary>
        /// <param name="context">Contexto da base de dados para registo de transações.</param>
        /// <param name="clientFactory">Fábrica de clientes HTTP.</param>
        /// <param name="userManager">Gestor de utilizadores para identificação do cliente e atualização de saldo.</param>
        public PaymentController(AppDbContext context, IHttpClientFactory clientFactory, UserManager<AppUser> userManager)
        {
            _context = context;
            _clientFactory = clientFactory;
            _userManager = userManager;
        }

        /// <summary>
        /// Apresenta a página inicial para escolha do valor de carregamento.
        /// </summary>
        /// <returns>A View de depósito.</returns>
        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        /// <summary>
        /// Cria uma sessão de Checkout no Stripe e regista uma transação pendente no sistema.
        /// </summary>
        /// <param name="amount">Valor monetário a carregar na conta.</param>
        /// <returns>Redirecionamento para o formulário de pagamento seguro do Stripe.</returns>
        /// <remarks>
        /// Gera uma referência única para a transação e define os URLs de retorno para sucesso ou cancelamento.
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(decimal amount)
        {
            if (amount <= 0)
                return BadRequest("Dados inválidos.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

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
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = (long)(Convert.ToDecimal(amount) * 100), // Stripe usa cêntimos
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

        /// <summary>
        /// Processa a confirmação de pagamento bem-sucedido vinda do Stripe.
        /// </summary>
        /// <param name="reference">Referência interna da transação gerada no início do processo.</param>
        /// <returns>Redireciona para a Home com mensagem de sucesso e saldo atualizado.</returns>
        /// <remarks>
        /// Este método valida a transação, marca-a como paga e incrementa o saldo (Balance) do utilizador.
        /// </remarks>
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

        /// <summary>
        /// Gere o retorno do utilizador quando o pagamento é cancelado ou interrompido.
        /// </summary>
        /// <returns>Redireciona para a Home com uma notificação de cancelamento.</returns>
        [HttpGet]
        public IActionResult CancelPayment()
        {
            TempData.SetSwalError("Pagamento cancelado.");
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
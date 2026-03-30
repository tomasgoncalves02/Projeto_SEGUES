using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.User;
using Stripe.Checkout;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Service implementation for handling financial transactions and Stripe integration.
/// Manages the creation of payment sessions and the subsequent validation of successful payments
/// to update user balances safely.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The application logger.</param>
    public PaymentService(AppDbContext context, ILogger<PaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Stripe Checkout Session and returns the hosted payment page URL.
    /// </summary>
    /// <param name="user">The user performing the top-up.</param>
    /// <param name="amount">The decimal amount to charge.</param>
    /// <param name="successUrl">URL to redirect after success.</param>
    /// <param name="cancelUrl">URL to redirect after cancellation.</param>
    /// <returns>The URL of the Stripe-hosted checkout page.</returns>
    /// <remarks>
    /// This method first persists a pending transaction in the database to track the intent.
    /// It then configures Stripe options, converting the decimal amount to cents (long) as required by the API.
    /// </remarks>
    public async Task<string> CreateStripeSessionAsync(AppUser user, decimal amount, string successUrl, string cancelUrl)
    {
        // Create a record of the intent to pay
        var transaction = new Transaction
        {
            User = user,
            Amount = amount,
        };

        _context.Transaction.Add(transaction);
        await _context.SaveChangesAsync();

        // Prepare return URLs with placeholders for Stripe to fill dynamically
        string finalSuccessUrl = successUrl
            .Replace("REF_PLACEHOLDER", transaction.Reference)
            .Replace("SESSION_PLACEHOLDER", "{CHECKOUT_SESSION_ID}"); // Stripe dynamic ID

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = (long)(amount * 100), // Stripe expects cents
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
            SuccessUrl = finalSuccessUrl,
            CancelUrl = cancelUrl
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    /// <summary>
    /// Processes the payment confirmation after the user returns from Stripe.
    /// Performs a double-check against Stripe's API to ensure the session is paid.
    /// </summary>
    /// <param name="reference">The internal SEGUES transaction reference.</param>
    /// <param name="sessionId">The external Stripe session identifier.</param>
    /// <returns>A ServiceResult indicating if the balance was successfully updated.</returns>
    /// <remarks>
    /// Uses a database transaction to ensure that the balance update and transaction 
    /// status change are atomic, preventing double-crediting.
    /// </remarks>
    public async Task<ServiceResult> ProcessPaymentSuccessAsync(string reference, string sessionId)
    {
        Session session;

        var service = new SessionService();
        try
        {
            // Security: Fetch the session directly from Stripe to verify status
            session = await service.GetAsync(sessionId);
        }
        catch (Exception)
        {
            _logger.LogAppUser($"Error retrieving Stripe session: {sessionId}. Ref: {reference}.", UserAction.FailedPayment);
            return ServiceResult.Fail("A sessão de pagamento é inválida ou expirou.");
        }

        // Verify if Stripe confirms the payment
        if (session.PaymentStatus != "paid")
        {
            _logger.LogAppUser($"Attempt to validate failed payment. Ref: {reference}.", UserAction.FailedPayment);
            return ServiceResult.Fail("O pagamento não foi concluído no Stripe.");
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Fetch the internal transaction and lock it for update
            var transaction = await _context.Transaction
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Reference == reference && !t.IsPaid);

            if (transaction == null)
            {
                _logger.LogAppUser($"Attempt to validate payment with invalid reference. Ref: {reference}",
                    UserAction.FailedPayment);
                return ServiceResult.Fail("Transação não encontrada ou já processada.");
            }

            // Update user balance and mark transaction as paid
            var user = transaction.User;
            user.Balance += transaction.Amount;
            transaction.IsPaid = true;

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogAppUser($"User {user.Email} added {transaction.Amount:C2} to account balance. Ref: {reference}.",
                UserAction.SuccessPayment);
            return ServiceResult.Ok("Saldo carregado com sucesso!");
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}
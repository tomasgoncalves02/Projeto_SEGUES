using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.User;
using Stripe.Checkout;

namespace Projeto_SEGUES.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    
    public PaymentService(AppDbContext context, ILogger<PaymentService> logger) {
        _context = context;
        _logger = logger;
    }
    
    public async Task<string> CreateStripeSessionAsync(AppUser user, decimal amount, string successUrl, string cancelUrl) 
    {
        var transaction = new Transaction {
            User = user,
            Amount = amount,
        };
            
        _context.Transaction.Add(transaction);
        await _context.SaveChangesAsync();
        
        string finalSuccessUrl = successUrl
            .Replace("REF_PLACEHOLDER", transaction.Reference)
            .Replace("SESSION_PLACEHOLDER", "{CHECKOUT_SESSION_ID}"); // Stripe needs the brackets
            
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
            SuccessUrl = finalSuccessUrl,
            CancelUrl = cancelUrl
        };
            
        var service = new SessionService();
        var session = await service.CreateAsync(options);
        
        return session.Url;
    }

    public async Task<ServiceResult> ProcessPaymentSuccessAsync(string reference, string sessionId)
    {
        Session session;
        
        var service = new SessionService();
        try
        {
            // Verify if payment was successful
            session = await service.GetAsync(sessionId);
        }
        catch (Exception)
        {
            _logger.LogAppUser($"Error retrieving Stripe session: {sessionId}. Ref: {reference}.", UserAction.FailedPayment);
            return ServiceResult.Fail("A sessão de pagamento é inválida ou expirou.");
        }

        if (session.PaymentStatus != "paid")
        {
            _logger.LogAppUser($"Attempt to validate failed payment. Ref: {reference}.", UserAction.FailedPayment);
            return ServiceResult.Fail("O pagamento não foi concluído no Stripe.");
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Fetch the transaction with the given reference
            var transaction = await _context.Transaction
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Reference == reference && !t.IsPaid);

            if (transaction == null)
            {
                _logger.LogAppUser($"Attempt to validate payment with invalid reference. Ref: {reference}",
                    UserAction.FailedPayment);
                return ServiceResult.Fail("Transação não encontrada ou já processada.");
            }

            // Update the transaction and user balance
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
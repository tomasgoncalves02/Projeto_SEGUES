using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public interface IPaymentService
{
    Task<string> CreateStripeSessionAsync(AppUser user, decimal amount, string successUrl, string cancelUrl);
    
    Task<ServiceResult> ProcessPaymentSuccessAsync(string reference, string sessionId);
}
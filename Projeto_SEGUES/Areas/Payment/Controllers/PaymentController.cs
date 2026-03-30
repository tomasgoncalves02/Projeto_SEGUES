using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Payment.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Payment.Controllers;

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
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<PaymentController> _logger;
    private readonly IPaymentService _paymentService;

    /// <summary>
    /// Initializes a new instance of the payment controller with database context, identity, logging, and localization.
    /// </summary>
    public PaymentController(
        UserManager<AppUser> userManager,
        ILogger<PaymentController> logger,
        IPaymentService paymentService)
    {
        _userManager = userManager;
        _logger = logger;
        _paymentService = paymentService;
    }

    /// <summary>
    /// Displays the initial page for choosing the top-up amount.
    /// </summary>
    /// <returns>The Deposit View.</returns>
    [HttpGet]
    public IActionResult Deposit() => View();

    /// <summary>
    /// Creates a Stripe Checkout session and records a pending transaction in the system.
    /// </summary>
    /// <param name="model">The view model containing the amount to be deposited.</param>
    /// <returns>Redirect to Stripe's secure payment form or an error page on failure.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCheckoutSession(DepositAmountViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData.SetSwalError("Por favor, corrija os erros no formulário.");
            return View("Deposit", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var successUrl = Url.Action("SuccessPayment", "Payment",
            new { reference = "REF_PLACEHOLDER", sessionId = "SESSION_PLACEHOLDER" }, Request.Scheme);
        var cancelUrl = Url.Action("CancelPayment", "Payment", null, Request.Scheme);
        
        try
        {
            var stripeUrl = await _paymentService.CreateStripeSessionAsync(user, model.Amount, successUrl!, cancelUrl!);
            return Redirect(stripeUrl);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogAppUser($"Payment gateway unreachable for user {user.Id}: {ex.Message}", UserAction.FailedPayment);
            TempData.SetSwalError("Não foi possível contactar o servidor de pagamentos. Verifique a sua ligação à internet.");
        }
        // InternalServerError bubbles up to the global error handler, which logs the error and shows a generic error page.
        
        return View("Deposit", model);
    }

    /// <summary>
    /// Processes the success confirmation from Stripe and updates user balance.
    /// </summary>
    /// <param name="reference">Internal transaction reference generated at the start.</param>
    /// <param name="sessionId">Stripe session ID used to identify the transaction.</param>
    /// <returns>Redirect to Home with success notification or error message.</returns>
    [HttpGet]
    public async Task<IActionResult> SuccessPayment(string reference, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(reference) || string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogAppError(AppErrors.BadRequest, TableName.All, AppOperation.Other);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.BadRequest });
        }
        
        var result = await _paymentService.ProcessPaymentSuccessAsync(reference, sessionId);
            
        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
        }
        else
        {
            TempData.SetSwalError(result.Message);
        }
            
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    /// <summary>
    /// Handles the return when a payment is canceled or interrupted.
    /// </summary>
    [HttpGet]
    public IActionResult CancelPayment()
    {
        TempData.SetSwalInfo("O processo de pagamento foi cancelado.");
        return RedirectToAction("Index", "Home", new { area = "" });
    }
}
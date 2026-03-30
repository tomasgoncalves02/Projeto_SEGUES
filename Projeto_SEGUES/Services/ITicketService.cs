using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Interface for the Meal Ticket Management Service.
/// Handles the lifecycle of meal tickets, including dynamic pricing, purchasing, 
/// secure peer-to-peer transfers, and canteen validation.
/// </summary>
public interface ITicketService
{

    /// <summary>Retrieves all valid and unused tickets belonging to a specific user.</summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of active tickets ready for consumption or transfer.</returns>
    Task<List<Ticket>> GetActiveTicketsAsync(string userId);

    /// <summary>
    /// Determines the current ticket price for a specific user based on their category (e.g., Student, Staff, External).
    /// </summary>
    /// <param name="user">The user entity requesting the price.</param>
    /// <returns>The decimal value of a single ticket according to the active price policy.</returns>
    Task<decimal> GetCurrentPriceForUserAsync(AppUser user);

    /// <summary>Retrieves all tickets (active and used) associated with a user.</summary>
    Task<List<Ticket>> GetUserTicketsAsync(string userId);

    /// <summary>
    /// Processes a bulk purchase of meal tickets, deducting the total from the user's balance.
    /// </summary>
    /// <param name="userId">The unique identifier of the buyer.</param>
    /// <param name="quantity">The number of tickets to purchase.</param>
    /// <returns>A ServiceResult indicating success or balance/pricing errors.</returns>
    Task<ServiceResult> BuyTicketsAsync(string userId, int quantity);


    /// <summary>
    /// Retrieves a historical record of tickets based on filtered criteria for reporting or personal history.
    /// </summary>
    /// <param name="userId">Optional user filter. If null, retrieves global history for administrators.</param>
    /// <param name="model">The filtering model containing date ranges and statuses.</param>
    /// <returns>A list of tickets matching the search parameters.</returns>
    Task<List<Ticket>> GetTicketHistoryAsync(string? userId, ReportTicketSearchViewModel model);


    /// <summary>
    /// Validates if a transfer can occur by checking the recipient's existence and eligibility.
    /// </summary>
    /// <param name="senderId">The ID of the user attempting to send tickets.</param>
    /// <param name="recipientEmail">The email address of the target user.</param>
    /// <returns>A ServiceResult containing the recipient's full name if eligible.</returns>
    Task<ServiceResult<string>> CheckTransferEligibilityAsync(string senderId, string recipientEmail);

    /// <summary>
    /// Executes the transfer of specific tickets between two users.
    /// </summary>
    /// <param name="senderId">The ID of the sender.</param>
    /// <param name="recipientEmail">The email of the recipient.</param>
    /// <param name="selectedTickets">A list of unique ticket codes to be transferred.</param>
    /// <returns>A ServiceResult confirming the ownership change.</returns>
    Task<ServiceResult> TransferTicketsAsync(string senderId, string recipientEmail, List<string> selectedTickets);


    /// <summary>Retrieves the most recently validated tickets for the staff monitoring dashboard.</summary>
    /// <param name="take">The number of recent records to retrieve (default is 10).</param>
    Task<List<Ticket>> GetRecentUsedTicketsAsync(int take = 10);

    /// <summary>
    /// Validates a meal ticket at the point of service, checking expiration, ownership, and previous usage.
    /// </summary>
    /// <param name="code">The alphanumeric or QR code presented.</param>
    /// <param name="validator">The staff member (Employee) performing the validation.</param>
    /// <returns>A ServiceResult confirming the meal is served or providing the rejection reason.</returns>
    Task<ServiceResult> ValidateTicketAsync(string code, AppUser validator);

}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin.Controllers;

/// <summary>
/// Controller responsible for global management of tickets, pricing, validity, and auditing.
/// </summary>
/// <remarks>
/// This central administrative hub allows the management of the meal ticket ecosystem, 
/// including dynamic pricing by category, service windows (Lunch/Dinner), 
/// and the global lifecycle configuration of digital assets.
/// </remarks>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminTicketManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ITicketService _ticketService;
    private readonly IPdfService _pdfService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminTicketManagementController"/> class.
    /// </summary>
    /// <param name="adminService">Service for administrative configurations and pricing.</param>
    /// <param name="ticketService">Service for ticket lifecycle and history retrieval.</param>
    /// <param name="pdfService">Service for high-fidelity document generation.</param>
    public AdminTicketManagementController(
        IAdminService adminService,
        ITicketService ticketService,
        IPdfService pdfService)
    {
        _adminService = adminService;
        _ticketService = ticketService;
        _pdfService = pdfService;
    }

    /// <summary>
    /// Displays the main ticket management dashboard.
    /// </summary>
    /// <remarks>
    /// Aggregates service schedules, current price rules, and global validity parameters 
    /// into a single administrative view.
    /// </remarks>
    /// <returns>A View with the <see cref="AdminTicketManagementViewModel"/>.</returns>
    public async Task<IActionResult> Index()
    {
        BarCanteenConfigViewModel barCanteenConfig = await _adminService.GetScheduleAsync();

        AdminTicketManagementViewModel vm = new AdminTicketManagementViewModel
        {
            LunchOpeningTime = barCanteenConfig.CanteenLunchOpeningTimeString!,
            LunchClosingTime = barCanteenConfig.CanteenLunchClosingTimeString!,
            DinnerOpeningTime = barCanteenConfig.CanteenDinnerOpeningTimeString!,
            DinnerClosingTime = barCanteenConfig.CanteenDinnerClosingTimeString!,
            Prices = await _adminService.GetTicketPricesAsync(),
            CurrentValidityDays = await _adminService.GetTicketValidityDaysAsync(),
            SearchModel = new ReportTicketSearchViewModel
            {
                // Loads the initial history batch for the audit table
                Results = await _ticketService.GetTicketHistoryAsync(null, new ReportTicketSearchViewModel())
            }
        };
        return View(vm);
    }

    /// <summary>
    /// Updates the ticket audit table via AJAX based on filter criteria.
    /// </summary>
    /// <param name="model">Search parameters bound from the UI filters.</param>
    /// <returns>A PartialView containing only the table rows for the audit trail.</returns>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedAuditTable([Bind(Prefix = "SearchModel")] ReportTicketSearchViewModel model)
    {
        var history = await _ticketService.GetTicketHistoryAsync(null, model);
        return PartialView("_AuditTableRows", history);
    }

    /// <summary>
    /// Updates the operating hours for a specific canteen service (Lunch or Dinner).
    /// </summary>
    /// <param name="serviceName">The identifier for the service (e.g., "Almoço").</param>
    /// <param name="openTime">New start time.</param>
    /// <param name="closeTime">New end time.</param>
    /// <returns>A redirect to Index with a success or error notification.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSchedule(string serviceName, TimeSpan openTime, TimeSpan closeTime)
    {
        BarCanteenConfigViewModel vm = serviceName == "Almoço"
            ? new() { CanteenLunchOpeningTime = openTime, CanteenLunchClosingTime = closeTime }
            : new() { CanteenDinnerOpeningTime = openTime, CanteenDinnerClosingTime = closeTime };

        var result = await _adminService.UpdateScheduleAsync(vm);
        if (result.Success)
            TempData.SetSwalSuccess(result.Message);
        else
            TempData.SetSwalError(result.Message);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Updates multiple ticket prices across different user categories.
    /// </summary>
    /// <param name="updatedPrices">A collection of DTOs containing the IDs and new prices.</param>
    /// <returns>A redirect to Index with the result of the batch operation.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrices([Bind(Prefix = "Prices")] List<TicketPriceUpdateDto> updatedPrices)
    {
        if (updatedPrices.Count == 0) return RedirectToAction(nameof(Index));

        var result = await _adminService.UpdateTicketPricesAsync(updatedPrices);
        if (result.Success)
            TempData.SetSwalSuccess(result.Message);
        else
            TempData.SetSwalError(result.Message);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Modifies the global validity period (in days) for future ticket purchases.
    /// </summary>
    /// <param name="validityDays">The new number of days before a ticket expires.</param>
    /// <returns>A redirect to the dashboard.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateValidity(int validityDays)
    {
        var result = await _adminService.UpdateTicketValidityDaysAsync(validityDays);
        if (result.Success)
            TempData.SetSwalSuccess(result.Message);
        else
            TempData.SetSwalError(result.Message);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Exports the filtered ticket audit trail to a PDF file.
    /// </summary>
    /// <param name="model">Filters to apply to the exported data.</param>
    /// <remarks>The resulting document uses a landscape layout for readability.</remarks>
    /// <returns>A PDF file download.</returns>
    [HttpGet]
    public async Task<IActionResult> ExportTicketsPdf([Bind(Prefix = "SearchModel")] ReportTicketSearchViewModel model)
    {
        var tickets = await _ticketService.GetTicketHistoryAsync(null, model);
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo-ips.png");
        byte[] pdfBytes = _pdfService.GenerateAdminTicketHistoryPdfAsync(tickets, logoPath);
        return File(pdfBytes, "application/pdf", $"Historico_Senhas_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
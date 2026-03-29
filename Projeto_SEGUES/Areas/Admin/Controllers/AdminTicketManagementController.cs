using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsible for global management of tickets, pricing, validity, and auditing.
/// </summary>
/// <remarks>
/// This controller allows administrators to configure meal prices, define service hours 
/// (lunch/dinner), manage ticket validity, and export audit reports.
/// </remarks>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminTicketManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ITicketService _ticketService;
    private readonly IPdfService _pdfService;

    /// <summary>
    /// Initializes a new instance of the controller with administration, user, ticket, logging, and localization services.
    /// </summary>
    /// <param name="adminService">Administrative configuration service.</param>
    /// <param name="ticketService">Ticket operations service.</param>
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
    /// Displays the main ticket management dashboard, including pricing, schedules, and history.
    /// </summary>
    /// <returns>The index View with the complete ticket history and configuration data in the ViewBag.</returns>
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
                Results = await _ticketService.GetTicketHistoryAsync(null, new ReportTicketSearchViewModel())
            }
        };
        return View(vm);
    }
    
    /// <summary>
    /// Filters the ticket history for dynamic updates of the audit table.
    /// </summary>
    /// <param name="model">ReportTicketSearchViewModel containing the filter criteria.</param>
    /// <returns>A PartialView containing the filtered table rows.</returns>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedAuditTable([Bind(Prefix = "SearchModel")] ReportTicketSearchViewModel model)
    {
        var history = await _ticketService.GetTicketHistoryAsync(null, model);
        return PartialView("_AuditTableRows", history);
    }
    
    /// <summary>
    /// Updates the operating hours of a specific service (Lunch or Dinner).
    /// </summary>
    /// <param name="serviceName">Name of the service to update.</param>
    /// <param name="openTime">Opening hour.</param>
    /// <param name="closeTime">Closing hour.</param>
    /// <returns>Redirects to Index informing success or validation error.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSchedule(string serviceName, TimeSpan openTime, TimeSpan closeTime)
    {
        BarCanteenConfigViewModel vm = serviceName == "Almoço"
            ? new() { CanteenLunchOpeningTime = openTime, CanteenLunchClosingTime = closeTime }
            : new() { CanteenDinnerOpeningTime = openTime, CanteenDinnerClosingTime = closeTime };
        
        var result = await _adminService.UpdateScheduleAsync(vm);
        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
        }
        else
        {
            TempData.SetSwalError(result.Message);
        }

        return RedirectToAction(nameof(Index));
    }
    

    /// <summary>
    /// Updates the ticket pricing values in the system.
    /// </summary>
    /// <param name="updatedPrices">List of TicketPrice models with the new values.</param>
    /// <returns>Redirects to Index with the result of the operation via SweetAlert.</returns>
    /// <remarks>
    /// Forces the Invariant culture for correct decimal processing and clears the ModelState to avoid validation conflicts.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrices([Bind(Prefix = "Prices")] List<TicketPriceUpdateDto> updatedPrices)
    {
        if (!updatedPrices.Any()) return RedirectToAction(nameof(Index));
        
        var result = await _adminService.UpdateTicketPricesAsync(updatedPrices);
        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
        }
        else
        {
            TempData.SetSwalError(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Changes the global validity period for newly purchased tickets.
    /// </summary>
    /// <param name="validityDays">Number of validity days (minimum 1).</param>
    /// <returns>Redirects to Index with confirmation or error.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateValidity(int validityDays)
    {
        var result = await _adminService.UpdateTicketValidityDaysAsync(validityDays);
        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
        }
        else
        {
            TempData.SetSwalError(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }
    
    /// <summary>
    /// Generates a detailed PDF report for auditing all tickets in the system.
    /// </summary>
    /// <returns>PDF file with ownership history, transfers, usage, and expiration.</returns>
    /// <remarks>
    /// Uses Landscape orientation to accommodate 9 data columns and includes official Teal styling.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> ExportTicketsPdf([Bind(Prefix = "SearchModel")] ReportTicketSearchViewModel model)
    {
        var tickets = await _ticketService.GetTicketHistoryAsync(null, model);
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo-ips.png");
        byte[] pdfBytes =  _pdfService.GenerateAdminTicketHistoryPdfAsync(tickets, logoPath);
        return File(pdfBytes, "application/pdf", $"Historico_Senhas_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
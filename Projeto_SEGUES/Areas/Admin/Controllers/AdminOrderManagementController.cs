using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsible for managing and monitoring orders and bar operating schedules.
/// </summary>
/// <remarks>
/// This controller provides administrative tools to oversee sales history, configure 
/// global operational parameters (like business hours), and export audited reports in PDF format.
/// Access is strictly restricted to users with the "Admin" role.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminOrderManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IReportService _reportService;
    private readonly IPdfService _pdfService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminOrderManagementController"/> class.
    /// </summary>
    /// <param name="adminService">Service handling global configuration and business hours logic.</param>
    /// <param name="reportService">Service providing filtered data for sales and order history.</param>
    /// <param name="pdfService">Service utilizing QuestPDF for document generation.</param>
    public AdminOrderManagementController(
        IAdminService adminService,
        IReportService reportService,
        IPdfService pdfService)
    {
        _reportService = reportService;
        _adminService = adminService;
        _pdfService = pdfService;
    }

    /// <summary>
    /// Displays the main administration dashboard for orders and schedules.
    /// </summary>
    /// <remarks>
    /// Aggregates current bar configurations and the initial history of orders.
    /// Sets a <c>ViewBag.ShowUser</c> flag to ensure the UI renders the customer details in the history grid.
    /// </remarks>
    /// <returns>A View with the <see cref="AdminOrderManagementViewModel"/> populated.</returns>
    public async Task<IActionResult> Index()
    {
        BarCanteenConfigViewModel barCanteenConfig = await _adminService.GetScheduleAsync();
        AdminOrderManagementViewModel vm = new AdminOrderManagementViewModel
        {
            BarOpeningTimeString = barCanteenConfig.BarOpeningTimeString!,
            BarClosingTimeString = barCanteenConfig.BarClosingTimeString!,

            IsOpenSaturday = barCanteenConfig.IsOpenSaturday,
            IsOpenSunday = barCanteenConfig.IsOpenSunday,

            SearchModel = new ReportOrderSearchViewModel()
        };

        // Fetch initial results for the dashboard
        vm.SearchModel.Results = await _reportService.GetAdminOrderHistoryAsync(vm.SearchModel);
        ViewBag.ShowUser = true;

        return View(vm);
    }

    /// <summary>
    /// Updates the bar's operational window (Opening and Closing hours).
    /// </summary>
    /// <param name="openTime">The start time of the daily service.</param>
    /// <param name="closeTime">The end time of the daily service.</param>
    /// <remarks>
    /// The service layer performs consistency checks: intervals must be positive and meet minimum duration rules.
    /// </remarks>
    /// <returns>A redirect to the Index with a success or error SweetAlert notification.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOpenAndCloseTime(TimeSpan openTime, TimeSpan closeTime)
    {
        var result = await _adminService.UpdateScheduleAsync(new BarCanteenConfigViewModel
        {
            BarOpeningTime = openTime,
            BarClosingTime = closeTime
        });

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
    /// Asynchronously retrieves a filtered subset of orders based on search criteria.
    /// </summary>
    /// <param name="model">The search model bound from the request parameters.</param>
    /// <remarks>
    /// Optimized for AJAX/HTMX calls. Returns a partial view representing only the table rows.
    /// </remarks>
    /// <returns>A Partial View with the filtered order history.</returns>
    [HttpGet]
    public async Task<IActionResult> GetFilteredOrders([Bind(Prefix = "SearchModel")] ReportOrderSearchViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Challenge();
        }

        model.Results = await _reportService.GetAdminOrderHistoryAsync(model);
        ViewBag.ShowUser = true;

        // Reuses the shared partial view from the Report area for UI consistency
        return PartialView("~/Areas/Report/Views/ReportOrder/_OrderHistoryRowsPartial.cshtml", model.Results);
    }

    /// <summary>
    /// Generates and streams a PDF document containing the audited order history.
    /// </summary>
    /// <param name="model">The search model to filter which records are exported.</param>
    /// <returns>A file stream containing the PDF document.</returns>
    [HttpGet]
    public async Task<IActionResult> ExportOrdersPdf([Bind(Prefix = "SearchModel")] ReportOrderSearchViewModel model)
    {
        // Fetch all orders matching the criteria (ignores pagination for full export)
        var orders = await _reportService.GetAdminOrderHistoryAsync(model, true);

        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo-ips.png");
        byte[] pdfBytes = _pdfService.GenerateAdminOrderHistoryPdfAsync(orders, logoPath);

        return File(pdfBytes, "application/pdf", $"Historico_Pedidos_{DateTime.Now:yyyyMMdd}.pdf");
    }

    /// <summary>
    /// Toggles the bar availability for specific weekend days.
    /// </summary>
    /// <param name="day">Target day string ("Saturday" or "Sunday").</param>
    /// <param name="isOpen">The new availability state.</param>
    /// <returns>A redirect to the Index action.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateWeekendStatus(string day, bool isOpen)
    {
        var result = await _adminService.UpdateSpecificDayStatusAsync(day, isOpen);

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
}
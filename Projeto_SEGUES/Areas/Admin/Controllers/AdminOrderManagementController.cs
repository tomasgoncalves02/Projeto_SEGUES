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
/// This controller allows administrators to view sales history, configure the bar's 
/// opening/closing times, and export detailed reports in PDF format.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminOrderManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IReportService _reportService;
    private readonly IPdfService _pdfService;

    /// <summary>
    /// Initializes a new instance of the controller with admin, order, user management services, logging, and localization.
    /// </summary>
    /// <param name="adminService">Administrative logic service.</param>
    /// <param name="reportService">Report service.</param>
    /// <param name="pdfService">PDF generation service.</param>
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
    /// Displays the main order management page, listing history and current schedules.
    /// </summary>
    /// <returns>The index View with the list of orders obtained via the service.</returns>
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
        vm.SearchModel.Results = await _reportService.GetAdminOrderHistoryAsync(vm.SearchModel);
        ViewBag.ShowUser = true;
        return View(vm);
    }

    /// <summary>
    /// Updates the bar's opening and closing hours with consistency validations.
    /// </summary>
    /// <param name="openTime">New opening time.</param>
    /// <param name="closeTime">New closing time.</param>
    /// <returns>Redirects to Index with a success or error message.</returns>
    /// <remarks>
    /// Validates if hours are equal, if closing time is before opening time, or if the interval is less than one hour.
    /// </remarks>
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
        return PartialView("~/Areas/Report/Views/ReportOrder/_OrderHistoryRowsPartial.cshtml", model.Results);
    }

    /// <summary>
    /// Generates and exports a PDF document with the filtered order history.
    /// </summary>
    /// <param name="model">The search model containing filter criteria for the orders to be included in the report.</param>
    /// <returns>A dynamically generated PDF file using the QuestPDF library.</returns>
    /// <remarks>
    /// The document includes the institutional logo, user details, purchased products, and pickup times.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> ExportOrdersPdf([Bind(Prefix = "SearchModel")] ReportOrderSearchViewModel model)
    {
        var orders = await _reportService.GetAdminOrderHistoryAsync(model, true);
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo-ips.png");
        byte[] pdfBytes = _pdfService.GenerateAdminOrderHistoryPdfAsync(orders, logoPath);

        return File(pdfBytes, "application/pdf", $"Historico_Pedidos_{DateTime.Now:yyyyMMdd}.pdf");
    }

    /// <summary>
    /// Updates the bar's availability during weekends.
    /// </summary>
    /// <param name="day">The specific day to update (e.g., "Saturday" or "Sunday").</param>
    /// <param name="isOpen">Indicates whether the bar should be open or closed on the specified day.</param>
    /// <returns>Redirects to Index with a success or error message.</returns>
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
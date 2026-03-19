using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
    private readonly UserManager<AppUser> _userManager;
    private readonly ITicketService _ticketService;

    /// <summary>
    /// Initializes a new instance of the controller with administration, user, and ticket services.
    /// </summary>
    /// <param name="adminService">Administrative configuration service.</param>
    /// <param name="userManager">Identity user manager.</param>
    /// <param name="ticketService">Ticket operations service.</param>
    public AdminTicketManagementController(IAdminService adminService, UserManager<AppUser> userManager, ITicketService ticketService)
    {
        _adminService = adminService;
        _userManager = userManager;
        _ticketService = ticketService;
    }

    /// <summary>
    /// Displays the main ticket management dashboard, including pricing, schedules, and history.
    /// </summary>
    /// <returns>The index View with the complete ticket history and configuration data in the ViewBag.</returns>
    public async Task<IActionResult> Index()
    {
        ViewBag.CurrentUserId = _userManager.GetUserId(User);

        ViewBag.Prices = await _adminService.GetTicketPricesAsync();
        ViewBag.CurrentValidityDays = await _adminService.GetTicketValidityDaysAsync();
        ViewBag.LunchOpenTime = await _adminService.GetOpenLunchTimeAsync();
        ViewBag.LunchCloseTime = await _adminService.GetCloseLunchTimeAsync();
        ViewBag.DinnerOpenTime = await _adminService.GetOpenDinnerTimeAsync();
        ViewBag.DinnerCloseTime = await _adminService.GetCloseDinnerTimeAsync();

        var history = await _ticketService.GetAllTicketsAsync();

        return View(history);
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
    public async Task<IActionResult> UpdatePrices(List<TicketPrice> updatedPrices)
    {
        if (updatedPrices == null || !updatedPrices.Any()) return RedirectToAction(nameof(Index));

        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        foreach (var key in ModelState.Keys.ToList()) ModelState.Remove(key);

        try
        {
            await _adminService.UpdateTicketPricesAsync(updatedPrices);
            TempData["SwalData"] = "{\"icon\":\"success\",\"title\":\"Sucesso\",\"text\":\"Preçário atualizado!\"}";
        }
        catch (Exception)
        {
            TempData["SwalData"] = "{\"icon\":\"error\",\"title\":\"Erro\",\"text\":\"Falha ao gravar.\"}";
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
        if (validityDays < 1)
        {
            TempData.SetSwalError("A validade deve ser de pelo menos 1 dia.");
            return RedirectToAction(nameof(Index));
        }

        await _adminService.UpdateTicketValidityDaysAsync(validityDays);
        TempData.SetSwalSuccess($"Validade global alterada para {validityDays} dias.");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Filters the ticket history for dynamic updates of the audit table.
    /// </summary>
    /// <param name="searchString">Search term (owner name or validation code).</param>
    /// <param name="stateFilter">Optional filter by ticket state.</param>
    /// <param name="dateFilter">Optional filter by purchase date.</param>
    /// <returns>A PartialView containing the filtered table rows.</returns>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedAuditTable(string searchString, Projeto_SEGUES.Models.Enums.TicketState? stateFilter, DateTime? dateFilter)
    {
        var history = await _ticketService.GetAllTicketsAsync();

        var query = history.AsQueryable();

        if (stateFilter.HasValue)
            query = query.Where(t => t.State == stateFilter.Value);

        if (dateFilter.HasValue)
            query = query.Where(t => t.TicketPurchase.TransactionDate.Date >= dateFilter.Value.Date);

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(t => t.Owner.FirstName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                                     t.Owner.LastName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                                     t.ValidationCode.Contains(searchString.ToUpper()));
        }

        return PartialView("_AuditTableRows", query.OrderByDescending(t => t.TicketPurchase.TransactionDate).ToList());
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
        if (openTime >= closeTime)
        {
            TempData.SetSwalError($"No serviço de {serviceName}, a abertura deve ser antes do fecho.");
            return RedirectToAction(nameof(Index));
        }

        await _adminService.UpdateBarScheduleAsync(openTime.ToString(), closeTime.ToString(), serviceName);

        TempData.SetSwalSuccess($"Horário de {serviceName} atualizado com sucesso!");
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
    public async Task<IActionResult> ExportTicketsPDF()
    {
        var history = await _ticketService.GetAllTicketsAsync();

        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo-ips.png");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);

                page.Header().PaddingBottom(10).Row(row =>
                {
                    if (System.IO.File.Exists(logoPath))
                    {
                        row.ConstantItem(150).Image(logoPath);
                    }

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text("Auditoria Geral de Senhas").FontSize(20).SemiBold().FontColor(Color.FromHex("#009697"));
                        col.Item().AlignRight().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).Italic();
                    });
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.5f); // Dono Atual
                        columns.ConstantColumn(60);   // Código
                        columns.ConstantColumn(70);   // Estado
                        columns.ConstantColumn(80);   // Compra
                        columns.ConstantColumn(60);   // Data Transf.
                        columns.RelativeColumn(1.5f); // Enviado Por
                        columns.RelativeColumn(1.5f); // Recebido Por
                        columns.ConstantColumn(60);   // Uso
                        columns.ConstantColumn(70);   // Expiração
                    });

                    table.Header(header =>
                    {
                        string[] colNames = { "Dono Atual", "Código", "Estado", "Compra", "Data Transf.", "Enviado Por", "Recebido Por", "Uso", "Expiração" };
                        foreach (var name in colNames)
                        {
                            header.Cell().Background(Color.FromHex("#009697")).Padding(5).AlignCenter().Text(name).FontColor(Colors.White).FontSize(8).SemiBold();
                        }
                    });

                    foreach (var t in history)
                    {
                        var lastTrans = t.Transfers?.OrderByDescending(x => x.TransferDate).FirstOrDefault();

                        table.Cell().Element(ContentStyle).AlignLeft().Column(c =>
                        {
                            c.Item().Text($"{t.Owner?.FirstName} {t.Owner?.LastName}").FontSize(8).SemiBold();
                            c.Item().Text(t.Owner?.Email).FontSize(7).FontColor(Colors.Grey.Medium);
                        });

                        table.Cell().Element(ContentStyle).Text(t.ValidationCode).FontFamily(Fonts.CourierNew).FontSize(8);
                        table.Cell().Element(ContentStyle).Text(t.IsUsed ? "Usada" : (t.ExpirationDate < DateTime.Now ? "Expirada" : "Disponível")).FontSize(8);
                        table.Cell().Element(ContentStyle).Text(t.TicketPurchase?.TransactionDate.ToString("dd/MM/yy HH:mm") ?? "-").FontSize(8);
                        table.Cell().Element(ContentStyle).Text(lastTrans?.TransferDate.ToString("dd/MM/yy") ?? "-").FontSize(8);
                        table.Cell().Element(ContentStyle).Text(lastTrans?.Sender?.UserName ?? "-").FontSize(8);
                        table.Cell().Element(ContentStyle).Text(lastTrans?.Receiver?.UserName ?? "Compra Direta").FontSize(8);
                        table.Cell().Element(ContentStyle).Text(t.IsUsed ? t.UsedDate?.ToString("dd/MM/yy") : "-").FontSize(8);
                        table.Cell().Element(ContentStyle).Text(t.ExpirationDate.ToString("dd/MM/yy")).FontSize(8);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página "); x.CurrentPageNumber();
                });
            });
        });

        return File(document.GeneratePdf(), "application/pdf", "Auditoria_Senhas_IPS.pdf");
    }

    /// <summary>
    /// Applies visual content styling to audit table cells.
    /// </summary>
    /// <param name="container">QuestPDF container to style.</param>
    /// <returns>The container with applied borders, padding, and alignment.</returns>
    static IContainer ContentStyle(IContainer container) =>
        container.PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).AlignCenter();
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;
    private readonly ILogger<AdminOrderManagementController> _logger;
    private readonly IStringLocalizer<Errors> _localizer;

    /// <summary>
    /// Initializes a new instance of the controller with admin, order, user management services, logging, and localization.
    /// </summary>
    /// <param name="adminService">Administrative logic service.</param>
    /// <param name="orderService">Order management service.</param>
    /// <param name="userManager">Identity user manager.</param>
    /// <param name="context">Entity Framework database context.</param>
    /// <param name="logger">Logger for error tracking and auditing.</param>
    /// <param name="localizer">Localizer for translating error messages.</param>
    public AdminOrderManagementController(
        IAdminService adminService,
        IOrderService orderService,
        UserManager<AppUser> userManager,
        AppDbContext context,
        ILogger<AdminOrderManagementController> logger,
        IStringLocalizer<Errors> localizer)
    {
        _orderService = orderService;
        _userManager = userManager;
        _adminService = adminService;
        _context = context;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Displays the main order management page, listing history and current schedules.
    /// </summary>
    /// <returns>The index View with the list of orders obtained via the service.</returns>
    public async Task<IActionResult> Index()
    {
        try
        {
            BarCanteenConfigViewModel barCanteenConfig = await _adminService.GetScheduleAsync();
            ViewBag.BarOpeningTimeString = barCanteenConfig.BarOpeningTimeString;
            ViewBag.BarClosingTimeString = barCanteenConfig.BarClosingTimeString;

            var orders = await _orderService.GetAdminOrderHistoryAsync();
            return View(orders);
        }
        catch (Exception ex)
        {
            // REDIRECT: Erro fatal ao carregar a página de gestão.
            _logger.LogError(ex, "Erro fatal ao carregar gestão de pedidos.");
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
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
        if (openTime == closeTime)
        {
            TempData.SetSwalError("A hora de abertura e de fecho não podem ser iguais.");
            return RedirectToAction(nameof(Index));
        }

        if (closeTime < openTime)
        {
            TempData.SetSwalError("A hora de fecho não pode ser anterior à hora de abertura.");
            return RedirectToAction(nameof(Index));
        }

        if ((closeTime - openTime).TotalHours < 1)
        {
            TempData.SetSwalError("O bar deve estar aberto pelo menos 1 hora.");
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _adminService.UpdateScheduleAsync(new BarCanteenConfigViewModel
            {
                BarOpeningTime = openTime,
                BarClosingTime = closeTime
            });
            TempData.SetSwalSuccess($"Horário de funcionamento do Bar alterado com sucesso.");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // SWEETALERT: Falha ao gravar na BD.
            _logger.LogAppError(AppErrors.DatabaseUpdateError, TableName.AppConfig, AppOperation.Update);

            var erroEnum = AppErrors.DatabaseUpdateError;
            var mensagemFinal = $"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]";

            TempData.SetSwalError(mensagemFinal);
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Generates and exports a PDF document with the filtered order history.
    /// </summary>
    /// <param name="status">Filter by order status.</param>
    /// <param name="date">Filter by specific date.</param>
    /// <param name="search">Search term (name, email, or code).</param>
    /// <returns>A dynamically generated PDF file using the QuestPDF library.</returns>
    /// <remarks>
    /// The document includes the institutional logo, user details, purchased products, and pickup times.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> ExportOrdersPDF(string status, DateTime? date, string search)
    {
        try
        {
            var query = _context.Order
                .Include(o => o.AppUser)
                .Include(o => o.ProductPurchases)
                    .ThenInclude(p => p.Product)
                .Where(o => o.Status != OrderStatus.Cart)
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(status)) query = query.Where(o => ((int)o.Status).ToString() == status);
            if (date.HasValue) query = query.Where(o => o.OrderDate.Date == date.Value.Date);
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(o => o.AppUser.FirstName.ToLower().Contains(search) ||
                                         o.AppUser.LastName.ToLower().Contains(search) ||
                                         o.RedemptionCode.ToLower().Contains(search) ||
                                         o.Id.ToString().Contains(search));
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo-ips.png");

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(15);

                    page.Header().Row(row =>
                    {
                        if (System.IO.File.Exists(logoPath)) row.ConstantItem(100).Image(logoPath);
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignRight().Text("Histórico Geral de Pedidos").FontSize(16).SemiBold().FontColor("#009697");
                            col.Item().AlignRight().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Italic();
                        });
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f);
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(60);
                            columns.RelativeColumn(2.5f);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(65);
                            columns.ConstantColumn(55);
                        });

                        table.Header(header =>
                        {
                            string[] titles = { "Utilizador", "Nº", "Código", "Data", "Agendado", "Produtos", "Estado", "Recolhido em", "Total" };
                            foreach (var t in titles)
                                header.Cell().Background("#009697").Padding(4).AlignCenter().Text(t).FontColor(Colors.White).FontSize(8).SemiBold();
                        });

                        foreach (var o in orders)
                        {
                            table.Cell().Element(CellStyle).Column(c =>
                            {
                                c.Item().Text($"{o.AppUser?.FirstName} {o.AppUser?.LastName}").FontSize(8).SemiBold();
                                c.Item().Text(o.AppUser?.Email).FontSize(7).FontColor(Colors.Grey.Medium);
                            });

                            table.Cell().Element(CellStyle).AlignCenter().Text($"#{o.Id:D5}");
                            table.Cell().Element(CellStyle).AlignCenter().Text(o.RedemptionCode).FontSize(7);
                            table.Cell().Element(CellStyle).AlignCenter().Text(o.OrderDate.ToString("dd/MM/yy HH:mm"));

                            table.Cell().Element(CellStyle).AlignCenter().Text(
                                (o.DeliveryTime.HasValue && o.DeliveryTime.Value != TimeSpan.Zero)
                                ? o.DeliveryTime.Value.ToString(@"hh\:mm")
                                : "Imediato"
                            );

                            table.Cell().Element(CellStyle).PaddingLeft(4).Column(c =>
                            {
                                foreach (var p in o.ProductPurchases)
                                    c.Item().Text($"• {p.Quantity}x {p.Product?.Name} ({p.ProductValue:N2}€)").FontSize(7);
                            });

                            table.Cell().Element(CellStyle).AlignCenter().Text(o.Status.ToString());

                            table.Cell().Element(CellStyle).AlignCenter().Text(
                                (o.PickupTime == null || o.PickupTime == TimeSpan.Zero)
                                ? "---"
                                : o.PickupTime.Value.ToString(@"hh\:mm")
                            );

                            table.Cell().Element(CellStyle).AlignRight().PaddingRight(4).Text($"{o.TotalValue:N2}€").SemiBold();
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página "); x.CurrentPageNumber();
                    });
                });
            });

            return File(document.GeneratePdf(), "application/pdf", "Historico_Pedidos.pdf");
        }
        catch (Exception ex)
        {           
            _logger.LogError(ex, "Falha na geração de PDF de pedidos.");

            var erroEnum = AppErrors.InternalServerError;
            var mensagemFinal = $"Não foi possível gerar o PDF. {_localizer[erroEnum.ToString()].Value} [{(int)erroEnum}]";

            TempData.SetSwalError(mensagemFinal);
            return RedirectToAction(nameof(Index), new { status, date, search });
        }
    }

    /// <summary>
    /// Applies a default style to table cells in the PDF report.
    /// </summary>
    /// <param name="container">Cell interface container.</param>
    /// <returns>The styled container with borders and padding.</returns>
    static IContainer CellStyle(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(4)
            .DefaultTextStyle(x => x.FontSize(8));
}
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
using QuestPDF.Previewer;

namespace Projeto_SEGUES.Areas.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminTicketManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ITicketService _ticketService;

    public AdminTicketManagementController(IAdminService adminService, UserManager<AppUser> userManager, ITicketService ticketService)
    {
        _adminService = adminService;
        _userManager = userManager;
        _ticketService = ticketService;
    }

    // Displays Prices + Global Ticket History
    public async Task<IActionResult> Index()
    {
        ViewBag.CurrentUserId = _userManager.GetUserId(User);

        ViewBag.Prices = await _adminService.GetTicketPricesAsync();
        ViewBag.CurrentValidityDays = await _adminService.GetTicketValidityDaysAsync();

        var history = await _ticketService.GetAllTicketsAsync();

        return View(history);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrices(List<TicketPrice> updatedPrices)
    {
        if (updatedPrices == null || !updatedPrices.Any()) return RedirectToAction(nameof(Index));

        // Forçar a cultura Invariante para que 1.50 seja lido como 1 euro e 50 cêntimos
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        // Removemos a validação automática para garantir que o código executa
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

    [HttpGet]
    public async Task<IActionResult> GetUpdatedAuditTable()
    {
        // Vai buscar os mesmos dados que a Index, mas retorna apenas a Partial
        var history = await _ticketService.GetAllTicketsAsync();

        // Importante: O nome deve coincidir com o ficheiro .cshtml que criaste
        return PartialView("_AuditTableRows", history);
    }

    [HttpGet]
    public async Task<IActionResult> ExportTicketsPDF()
    {
        // 1. Obter os dados (Garante que o serviço faz Include das relações)
        var history = await _ticketService.GetAllTicketsAsync();

        // Caminho para o logótipo
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo-ips.png");

        // 2. Criar o Documento PDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Definir Paisagem para as 9 colunas caberem
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);

                // --- CABEÇALHO COM LOGÓTIPO ---
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

                // --- TABELA DE 9 COLUNAS ---
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

                    // Cabeçalho da Tabela (Fundo Teal)
                    table.Header(header =>
                    {
                        string[] colNames = { "Dono Atual", "Código", "Estado", "Compra", "Data Transf.", "Enviado Por", "Recebido Por", "Uso", "Expiração" };
                        foreach (var name in colNames)
                        {
                            // CORREÇÃO: Background aplicado à Cell
                            header.Cell().Background(Color.FromHex("#009697")).Padding(5).AlignCenter().Text(name).FontColor(Colors.White).FontSize(8).SemiBold();
                        }
                    });

                    // Dados
                    foreach (var t in history)
                    {
                        var lastTrans = t.Transfers?.OrderByDescending(x => x.TransferDate).FirstOrDefault();

                        table.Cell().Element(ContentStyle).AlignLeft().Column(c => {
                            c.Item().Text($"{t.Owner?.FirstName} {t.Owner?.LastName}").FontSize(8).SemiBold();
                            c.Item().Text(t.Owner?.Email).FontSize(7).FontColor(Colors.Grey.Medium);
                        });

                        // Propriedade correta é ValidationCode
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

                page.Footer().AlignCenter().Text(x => {
                    x.Span("Página "); x.CurrentPageNumber();
                });
            });
        });

        return File(document.GeneratePdf(), "application/pdf", "Auditoria_Senhas_IPS.pdf");
    }

    static IContainer ContentStyle(IContainer container) =>
        container.PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).AlignCenter();
}
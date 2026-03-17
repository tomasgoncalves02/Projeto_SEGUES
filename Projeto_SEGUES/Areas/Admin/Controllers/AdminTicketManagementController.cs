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
/// Controller responsável pela gestão global de senhas (tickets), preçários, validade e auditoria.
/// </summary>
/// <remarks>
/// Este controlador permite aos administradores configurar os preços das refeições, definir horários 
/// de serviço (almoço/jantar), gerir a validade das senhas e exportar relatórios de auditoria.
/// </remarks>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminTicketManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ITicketService _ticketService;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de administração, utilizadores e senhas.
    /// </summary>
    /// <param name="adminService">Serviço de configuração administrativa.</param>
    /// <param name="userManager">Gestor de utilizadores Identity.</param>
    /// <param name="ticketService">Serviço de operações de senhas.</param>
    public AdminTicketManagementController(IAdminService adminService, UserManager<AppUser> userManager, ITicketService ticketService)
    {
        _adminService = adminService;
        _userManager = userManager;
        _ticketService = ticketService;
    }

    /// <summary>
    /// Apresenta o painel principal de gestão de senhas, incluindo preçários, horários e histórico.
    /// </summary>
    /// <returns>A View de índice com o histórico completo de senhas e dados de configuração no ViewBag.</returns>
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
    /// Atualiza os valores do preçário das senhas no sistema.
    /// </summary>
    /// <param name="updatedPrices">Lista de modelos TicketPrice com os novos valores.</param>
    /// <returns>Redireciona para o Index com o resultado da operação via SweetAlert.</returns>
    /// <remarks>
    /// Força a cultura Invariante para processamento correto de decimais e limpa o ModelState para evitar conflitos de validação.
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
    /// Altera o período de validade global para novas senhas adquiridas.
    /// </summary>
    /// <param name="validityDays">Número de dias de validade (mínimo 1).</param>
    /// <returns>Redireciona para o Index com a confirmação ou erro.</returns>
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
    /// Filtra o histórico de senhas para atualização dinâmica da tabela de auditoria.
    /// </summary>
    /// <param name="searchString">Termo de pesquisa (nome do dono ou código de validação).</param>
    /// <param name="stateFilter">Filtro opcional por estado da senha.</param>
    /// <param name="dateFilter">Filtro opcional por data de compra.</param>
    /// <returns>Uma PartialView contendo as linhas filtradas da tabela.</returns>
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
    /// Atualiza os horários de funcionamento de um serviço específico (Almoço ou Jantar).
    /// </summary>
    /// <param name="serviceName">Nome do serviço a atualizar.</param>
    /// <param name="openTime">Hora de abertura.</param>
    /// <param name="closeTime">Hora de fecho.</param>
    /// <returns>Redireciona para o Index informando o sucesso ou erro de validação.</returns>
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
    /// Gera um relatório PDF detalhado para auditoria de todas as senhas do sistema.
    /// </summary>
    /// <returns>Ficheiro PDF com histórico de propriedade, transferências, utilização e expiração.</returns>
    /// <remarks>
    /// Utiliza orientação Landscape para acomodar as 9 colunas de dados e inclui estilização Teal oficial.
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
    /// Aplica o estilo de conteúdo visual às células da tabela de auditoria.
    /// </summary>
    /// <param name="container">Contentor QuestPDF a estilizar.</param>
    /// <returns>O contentor com bordas, preenchimento e alinhamento aplicados.</returns>
    static IContainer ContentStyle(IContainer container) =>
        container.PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).AlignCenter();
}
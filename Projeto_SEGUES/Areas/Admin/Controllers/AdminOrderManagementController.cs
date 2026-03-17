using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsável pela gestão e monitorização de pedidos (orders) e horários do bar.
/// </summary>
/// <remarks>
/// Este controlador permite aos administradores visualizar o histórico de vendas, configurar o horário 
/// de funcionamento do bar e exportar relatórios detalhados em formato PDF.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminOrderManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de administração, pedidos, gestão de utilizadores e contexto de dados.
    /// </summary>
    /// <param name="adminService">Serviço de lógica administrativa.</param>
    /// <param name="orderService">Serviço de gestão de pedidos.</param>
    /// <param name="userManager">Gestor de utilizadores do Identity.</param>
    /// <param name="context">Contexto da base de dados Entity Framework.</param>
    public AdminOrderManagementController(IAdminService adminService, IOrderService orderService, UserManager<AppUser> userManager, AppDbContext context)
    {
        _orderService = orderService;
        _userManager = userManager;
        _adminService = adminService;
        _context = context;
    }

    /// <summary>
    /// Apresenta a página principal de gestão de pedidos, listando o histórico e horários atuais.
    /// </summary>
    /// <returns>A View de índice com a lista de pedidos obtida via serviço.</returns>
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        ViewBag.OpenBarTime = await _adminService.GetOpenBarTimeAsync();
        ViewBag.CloseBarTime = await _adminService.GetCloseBarTimesAsync();
        return View(await _orderService.GetAdminOrderHistoryAsync());
    }

    /// <summary>
    /// Atualiza as horas de abertura e fecho do bar com validações de consistência.
    /// </summary>
    /// <param name="openTime">Nova hora de abertura.</param>
    /// <param name="closeTime">Nova hora de fecho.</param>
    /// <returns>Redireciona para o Index com mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Valida se as horas são iguais, se o fecho é anterior à abertura ou se o intervalo é inferior a uma hora.
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

        await _adminService.UpdateBarScheduleAsync(openTime.ToString(), closeTime.ToString());
        TempData.SetSwalSuccess($"Horario de funcionamento do Bar alterado com sucessso");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Gera e exporta um documento PDF com o histórico de pedidos filtrado.
    /// </summary>
    /// <param name="status">Filtro por estado do pedido.</param>
    /// <param name="date">Filtro por data específica.</param>
    /// <param name="search">Termo de pesquisa (nome, email ou código).</param>
    /// <returns>Um ficheiro PDF gerado dinamicamente com a biblioteca QuestPDF.</returns>
    /// <remarks>
    /// O documento inclui logotipo institucional, detalhes de utilizador, produtos comprados e tempos de recolha.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> ExportOrdersPDF(string status, DateTime? date, string search)
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
                        columns.RelativeColumn(1.5f); // Utilizador
                        columns.ConstantColumn(40);   // Nº
                        columns.ConstantColumn(60);   // Código
                        columns.ConstantColumn(75);   // Data Compra
                        columns.ConstantColumn(60);   // Agendado (DeliveryTime)
                        columns.RelativeColumn(2.5f); // Produtos
                        columns.ConstantColumn(75);   // Estado
                        columns.ConstantColumn(65);   // Recolhido em (PickupTime)
                        columns.ConstantColumn(55);   // Valor
                    });

                    table.Header(header =>
                    {
                        string[] titles = { "Utilizador", "Nº", "Código", "Data", "Agendado", "Produtos", "Estado", "Recolhido em", "Total" };
                        foreach (var t in titles)
                            header.Cell().Background("#009697").Padding(4).AlignCenter().Text(t).FontColor(Colors.White).FontSize(8).SemiBold();
                    });

                    foreach (var o in orders)
                    {
                        // Utilizador
                        table.Cell().Element(CellStyle).Column(c =>
                        {
                            c.Item().Text($"{o.AppUser?.FirstName} {o.AppUser?.LastName}").FontSize(8).SemiBold();
                            c.Item().Text(o.AppUser?.Email).FontSize(7).FontColor(Colors.Grey.Medium);
                        });

                        table.Cell().Element(CellStyle).AlignCenter().Text($"#{o.Id:D5}");
                        table.Cell().Element(CellStyle).AlignCenter().Text(o.RedemptionCode).FontSize(7);
                        table.Cell().Element(CellStyle).AlignCenter().Text(o.OrderDate.ToString("dd/MM/yy HH:mm"));

                        // Agendado (DeliveryTime)
                        table.Cell().Element(CellStyle).AlignCenter().Text(
                            (o.DeliveryTime.HasValue && o.DeliveryTime.Value != TimeSpan.Zero)
                            ? o.DeliveryTime.Value.ToString(@"hh\:mm")
                            : "Imediato"
                        );

                        // Produtos
                        table.Cell().Element(CellStyle).PaddingLeft(4).Column(c =>
                        {
                            foreach (var p in o.ProductPurchases)
                                c.Item().Text($"• {p.Quantity}x {p.Product?.Name} ({p.ProductValue:N2}€)").FontSize(7);
                        });

                        table.Cell().Element(CellStyle).AlignCenter().Text(o.Status.ToString());

                        // Recolhido em (PickupTime)
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

    /// <summary>
    /// Aplica um estilo padrão às células das tabelas do relatório PDF.
    /// </summary>
    /// <param name="container">Contentor de interface da célula.</param>
    /// <returns>O contentor estilizado com bordas e preenchimento.</returns>
    static IContainer CellStyle(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(4)
            .DefaultTextStyle(x => x.FontSize(8));
}
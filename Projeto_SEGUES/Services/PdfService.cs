using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Order;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Projeto_SEGUES.Services;

public class PdfService : IPdfService
{
    private static readonly string _fontFamily = "Roboto";
    private static readonly string _primaryColor = "#009697";
    
    /// <summary>
    /// Auxiliary method for styling the cells.
    /// </summary>
    private static IContainer CellStyle(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(4)
            .DefaultTextStyle(x => x.FontFamily(_fontFamily).FontSize(8));
    
    /// <summary>
    /// Gera um relatório PDF com o histórico de pedidos filtrado.
    /// </summary>
    public async Task<byte[]> GenerateAdminOrderHistoryPdfAsync(List<Order> orders, string logoPath)
    {
        // Create pdf
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontFamily(_fontFamily).FontSize(9));
                
                // Page header
                page.Header().Row(row =>
                {
                    if (File.Exists(logoPath))
                        row.ConstantItem(100).Image(logoPath);

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text("Histórico Geral de Pedidos").FontSize(16).SemiBold().FontColor(_primaryColor);
                        col.Item().AlignRight().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Italic();
                    });
                });
                
                // Content
                page.Content().PaddingTop(10).Table(table =>
                {
                    // Define the columns widths
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.5f); // Utilizador
                        columns.ConstantColumn(40);   // Nº
                        columns.ConstantColumn(60);   // Código
                        columns.ConstantColumn(75);   // Data
                        columns.ConstantColumn(60);   // Agendado
                        columns.ConstantColumn(75);   // Estado
                        columns.ConstantColumn(65);   // Recolhido
                        columns.RelativeColumn(2.5f); // Produtos
                        columns.ConstantColumn(55);   // Total
                    });

                    // Table header
                    table.Header(header =>
                    {
                        string[] titles = { "Utilizador", "Nº", "Código", "Data", "Agendado", "Estado", "Recolhido em", "Produtos", "Total" };
                        foreach (var t in titles)
                        {
                            header.Cell().Background(_primaryColor).Padding(4).AlignCenter()
                                  .Text(t).FontColor(Colors.White).FontSize(8).SemiBold();
                        }
                    });

                    foreach (var o in orders)
                    {
                        // User
                        table.Cell().Element(CellStyle).Column(c =>
                        {
                            c.Item().Text($"{o.AppUser.FirstName} {o.AppUser.LastName}").FontSize(8).SemiBold();
                            c.Item().Text(o.AppUser.Email).FontSize(7).FontColor(Colors.Grey.Medium);
                        });

                        // Number
                        table.Cell().Element(CellStyle).AlignCenter().Text($"#{o.Id:D5}");
                        
                        // Code
                        table.Cell().Element(CellStyle).AlignCenter().Text(o.RedemptionCode).FontSize(7);
                        
                        // OrderDate
                        table.Cell().Element(CellStyle).AlignCenter().Text(o.OrderDate.ToString("dd/MM/yy HH:mm"));
                        
                        // DeliveryTime
                        table.Cell().Element(CellStyle).AlignCenter().Text(
                            (o.DeliveryTime.HasValue && o.DeliveryTime.Value != TimeSpan.Zero)
                            ? o.DeliveryTime.Value.ToString(@"hh\:mm")
                            : "Agora"
                        );
                        
                        // Status
                        table.Cell().Element(CellStyle).AlignCenter().Text(o.Status.ToDisplayName());
                        
                        // PickupTime
                        table.Cell().Element(CellStyle).AlignCenter().Text(
                            (o.PickupTime == null || o.PickupTime == TimeSpan.Zero)
                                ? "---"
                                : o.PickupTime.Value.ToString(@"hh\:mm")
                        );
                        
                        // Products
                        table.Cell().Element(CellStyle).PaddingLeft(4).Column(c =>
                        {
                            foreach (var p in o.ProductPurchases)
                                c.Item().Text($"• {p.Quantity}x {p.Product.Name} ({p.ProductValue:C})").FontSize(7);
                        });

                        // Total
                        table.Cell().Element(CellStyle).AlignRight().PaddingRight(4).Text($"{o.TotalValue:C}").SemiBold();
                    }
                });
                
                page.Footer().PaddingTop(5).AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        // return the PDF as a byte array
        return document.GeneratePdf();
    }
}
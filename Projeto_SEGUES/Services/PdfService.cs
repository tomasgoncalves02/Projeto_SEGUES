using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Projeto_SEGUES.Services;

public class PdfService : IPdfService
{
    private static readonly string FontFamily = "Roboto";
    private static readonly string PrimaryColor = "#009697";
    
    /// <summary>
    /// Configures the default page settings (size, margins, colors, fonts) for the PDF document.
    /// </summary>
    /// <param name="page">The page descriptor to configure.</param>
    private static void ConfigureDefaultPage(PageDescriptor page)
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(15);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(9));
    }
    
    /// <summary>
    /// Composes the header section of the PDF document.
    /// </summary>
    /// <param name="container">The container to compose the header elements into.</param>
    /// <param name="title">The title text to display in the header.</param>
    /// <param name="logoPath">The file path to the logo image to display in the header (optional).</param>
    private static void ComposeHeader(IContainer container, string title, string logoPath)
    {
        container.PaddingBottom(10).Row(row =>
        {
            if (File.Exists(logoPath))
                row.ConstantItem(100).Image(logoPath);

            row.RelativeItem().Column(col =>
            {
                col.Item().AlignRight().Text(title).FontSize(16).SemiBold().FontColor(PrimaryColor);
                col.Item().AlignRight().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Italic();
            });
        });
    }
    
    /// <summary>
    /// Composes the footer section of the PDF document.
    /// </summary>
    /// <param name="container">The container to compose the footer elements into.</param>
    private static void ComposeFooter(IContainer container)
    {
        container.PaddingTop(5).AlignCenter().Text(x =>
        {
            x.Span("Página ");
            x.CurrentPageNumber();
            x.Span(" de ");
            x.TotalPages();
        });
    }
    
    /// <summary>
    /// Auxiliary method for adding a header cell to a table.
    /// </summary>
    /// <param name="table">The table descriptor to add the header cell to.</param>
    /// <param name="text">The text content of the header cell.</param>
    private static void AddHeaderCell(TableDescriptor table, string text)
    {
        table.Cell().Background(PrimaryColor).Padding(4).AlignCenter()
            .Text(text).FontColor(Colors.White).FontSize(8).SemiBold();
    }
    
    /// <summary>
    /// Auxiliary method for drawing a date cell in a table.
    /// </summary>
    /// <param name="container">The container to compose the date cell elements into.</param>
    /// <param name="date">The DateTime value to display in the cell.</param>
    private static void DrawDateCell(IContainer container, DateTime date)
    {
        container.Column(c => 
        {
            c.Item().AlignCenter().Text(date.ToString("dd/MM/yyyy")).FontSize(8).SemiBold();
            c.Item().AlignCenter().Text(date.ToString("HH:mm")).FontSize(7).FontColor(Colors.Grey.Medium);
        });
    }

    /// <summary>
    /// Auxiliary method for drawing a user cell in a table.
    /// </summary>
    /// <param name="container">The container to compose the user cell elements into.</param>
    /// <param name="user">The AppUser object containing the user's information to display in the cell.</param>
    private static void DrawUserCell(IContainer container, AppUser user)
    {
        container.ExtendHorizontal().AlignLeft().PaddingLeft(4).Column(c => 
        {
            c.Item().AlignLeft().Text($"{user.FirstName} {user.LastName}").FontSize(8).SemiBold();
            c.Item().AlignLeft().Text(user.Email).FontSize(7).FontColor(Colors.Grey.Medium);
        });   
    }
    
    /// <summary>
    /// Auxiliary method for drawing a products cell in a table.
    /// </summary>
    /// <param name="container">The container to compose the products cell elements into.</param>
    /// <param name="products">The collection of OrderLine objects representing the products to display in the cell.</param>
    private static void DrawProductsCell(IContainer container, IEnumerable<OrderLine> products)
    {
        container.ExtendHorizontal().AlignLeft().PaddingLeft(4).Column(c =>
        {
            foreach (var p in products)
                c.Item().Text($"• {p.Quantity}x {p.Product.Name} ({p.ProductValue:C})").FontSize(7);
        });
    }
    
    /// <summary>
    /// Auxiliary method for styling the cells.
    /// </summary>
    private static IContainer CellStyle(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(4)
            .AlignMiddle()
            .AlignCenter()
            .DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(8));
    
    /// <summary>
    /// Gera um relatório PDF com o histórico de pedidos filtrado.
    /// </summary>
    public byte[] GenerateAdminOrderHistoryPdfAsync(List<Order> orders, string logoPath)
    {
        // Create pdf
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Global configuration
                ConfigureDefaultPage(page);
                page.Header().Element(c => ComposeHeader(c, "Histórico de Pedidos", logoPath));
                page.Footer().Element(ComposeFooter);
                
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
                    table.Header(_ =>
                    {
                        string[] titles = { "Utilizador", "Nº", "Código", "Data", "Agendado", "Estado", "Recolhido em", "Produtos", "Total" };
                        foreach (var t in titles) AddHeaderCell(table, t);
                    });

                    foreach (var o in orders)
                    {
                        // User
                        table.Cell().Element(CellStyle).Element(c => DrawUserCell(c, o.AppUser));

                        // Number
                        table.Cell().Element(CellStyle).Text($"#{o.Id:D5}");
                        
                        // Code
                        table.Cell().Element(CellStyle).Text(o.RedemptionCode).FontSize(7);
                        
                        // OrderDate
                        table.Cell().Element(CellStyle).Element(c => DrawDateCell(c, o.OrderDate));
                        
                        // DeliveryTime
                        table.Cell().Element(CellStyle).Text(
                            (o.DeliveryTime.HasValue && o.DeliveryTime.Value != TimeSpan.Zero)
                            ? o.DeliveryTime.Value.ToString(@"hh\:mm")
                            : "Agora"
                        );
                        
                        // Status
                        table.Cell().Element(CellStyle).Text(o.Status.ToDisplayName());
                        
                        // PickupTime
                        table.Cell().Element(CellStyle).Text(
                            (o.PickupTime == null || o.PickupTime == TimeSpan.Zero)
                                ? "---"
                                : o.PickupTime.Value.ToString(@"hh\:mm")
                        );
                        
                        // Products
                        table.Cell().Element(CellStyle).Element(c => DrawProductsCell(c, o.ProductPurchases));

                        // Total
                        table.Cell().Element(CellStyle).AlignRight().PaddingRight(4).Text($"{o.TotalValue:C}").SemiBold();
                    }
                });
                
            });
        });

        // return the PDF as a byte array
        return document.GeneratePdf();
    }

    public byte[] GenerateAdminTicketHistoryPdfAsync(List<Ticket> tickets, string logoPath)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Global configuration
                ConfigureDefaultPage(page);
                page.Header().Element(c => ComposeHeader(c, "Histórico de Senhas", logoPath));
                page.Footer().Element(ComposeFooter);

                // Table content
                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.8f); // Titular
                        columns.ConstantColumn(50);   // Código
                        columns.ConstantColumn(65);   // Compra
                        columns.ConstantColumn(60);   // Estado
                        columns.ConstantColumn(65);   // Data Transf.
                        columns.RelativeColumn(1.5f); // Enviado Por
                        columns.RelativeColumn(1.5f); // Recebido Por
                        columns.ConstantColumn(60);   // Utilização
                        columns.ConstantColumn(60);   // Validade
                    });

                    table.Header(_ =>
                    {
                        string[] colNames = { "Titular", "Código", "Compra", "Estado", "Data Transf.", "Enviado Por", "Recebido Por", "Utilização", "Validade" };
                        foreach (var name in colNames) AddHeaderCell(table, name);
                    });

                    foreach (var t in tickets)
                    {
                        var lastTrans = t.Transfers.OrderByDescending(x => x.TransferDate).FirstOrDefault();
                        
                        // Owner
                        table.Cell().Element(CellStyle).Element(c => DrawUserCell(c, t.Owner));
                        
                        // Code
                        table.Cell().Element(CellStyle).Text(t.ValidationCode).FontSize(7);
                        
                        // Purchase Date
                        table.Cell().Element(CellStyle).Element(c => DrawDateCell(c, t.TicketPurchase.TransactionDate));
                        
                        // State
                        table.Cell().Element(CellStyle).Text(t.State.ToDisplayName());
                        
                        // Transfer
                        if (lastTrans == null)
                        {
                            table.Cell().Element(CellStyle).Text("---").FontColor(Colors.Grey.Medium).Italic(); // Date
                            table.Cell().Element(CellStyle).Text("---").FontColor(Colors.Grey.Medium).Italic(); // Sender
                            table.Cell().Element(CellStyle).Text("Compra Direta").FontColor(Colors.Grey.Medium).Italic(); // Receiver
                        }
                        else
                        {
                            // Date of transfer
                            table.Cell().Element(CellStyle).Element(c => DrawDateCell(c, lastTrans.TransferDate));
                            
                            // Sender
                            table.Cell().Element(CellStyle).Element(c => DrawUserCell(c, lastTrans.Sender));
                            
                            // Receiver
                            table.Cell().Element(CellStyle).Element(c => DrawUserCell(c, lastTrans.Receiver));
                        }
                        
                        // Used date
                        if (t.UsedDate == null || t.IsUsed == false)
                        {
                            table.Cell().Element(CellStyle).Text("---").FontColor(Colors.Grey.Medium).Italic();
                        }
                        else
                        {
                            table.Cell().Element(CellStyle).Element(c => DrawDateCell(c, t.UsedDate.Value));
                        }
                        
                        // Expiration date
                        table.Cell().Element(CellStyle).Text(t.ExpirationDate.ToString("dd/MM/yyyy"));
                    }
                });
            });
        });

        return document.GeneratePdf();
    }
}
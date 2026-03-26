namespace Projeto_SEGUES.Areas.Inventory.ViewModels;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int CategoryId { get; set; }
    
    public decimal Price { get; set; }
    public string FormattedPrice => Price.ToString("C");
    
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public bool IsActive { get; set; }
    
    public string InactiveRowClass => IsActive ? "" : "text-muted text-decoration-line-through";
    public string StockBadgeClass => Stock <= 0 ? "bg-danger" : (Stock < MinimumStock ? "bg-warning" : "bg-success");

    // JSON
    public object ModalInfo { get; set; } = null!;
}
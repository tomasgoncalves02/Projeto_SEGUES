namespace Projeto_SEGUES.Areas.Order.ViewModels;

public class OrderProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string FormattedPrice => Price.ToString("C");
    public int Quantity { get; set; }
    
    public decimal TotalPrice => Price * Quantity;
    public string FormattedTotalPrice => TotalPrice.ToString("C");
    
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    
    // Anonymous object for javascript
    public object ModalInfo { get; set; } = null!;
}
namespace Projeto_SEGUES.Areas.Order.ViewModels;

public class OrderDetailsViewModel
{
    public Models.Order.Order Order { get; set; }
    public string FormattedTotalValue => Order.TotalValue.ToString("C");
    public string FormattedOrderDate => Order.OrderDate.ToString(@"HH\:mm");
    public string FormattedDeliveryDate => Order.DeliveryTime.HasValue ? Order.DeliveryTime.Value.ToString(@"hh\:mm") : "";
    public int TotalQuantity { get; set; }
    
    public bool IsScheduled => Order.DeliveryTime.HasValue && Order.DeliveryTime.Value != TimeSpan.Zero;
    
    public IEnumerable<OrderProductDto> Items { get; set; } = new List<OrderProductDto>();
}
namespace Projeto_SEGUES.Areas.Order.ViewModels;

public class OrderManagementSideViewModel
{
    public int Id { get; set; }
    public string FormattedTotalValue { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public string BuyerName { get; set; } = string.Empty;

    // Status
    public int CurrentStatusId { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    
    public int PrevStatusId { get; set; }
    public bool CanGoBack { get; set; }
    
    public int NextStatusId { get; set; }
    public bool CanGoForward { get; set; }

    // List of items in the order
    public IEnumerable<OrderProductDto> Items { get; set; } = new List<OrderProductDto>();
}
namespace Projeto_SEGUES.Areas.Order.ViewModels;

/// <summary>
/// ViewModel used for the detailed display of an existing order.
/// </summary>
/// <remarks>
/// This model extends the base order information with formatted strings for the UI 
/// and calculated properties to handle delivery scheduling status.
/// </remarks>
public class OrderDetailsViewModel
{
    /// <summary>
    /// The core order entity containing transaction and status data.
    /// </summary>
    public required Models.Order.Order Order { get; set; }

    /// <summary>
    /// Total monetary value of the order formatted as a currency string.
    /// </summary>
    public string FormattedTotalValue => Order.TotalValue.ToString("C");

    /// <summary>
    /// Time when the order was placed, formatted for short display (HH:mm).
    /// </summary>
    public string FormattedOrderDate => Order.OrderDate.ToString(@"HH\:mm");

    /// <summary>
    /// The scheduled delivery or pickup time, formatted as a string if available.
    /// </summary>
    public string FormattedDeliveryDate => Order.DeliveryTime.HasValue ? Order.DeliveryTime.Value.ToString(@"hh\:mm") : "";

    /// <summary>
    /// Cumulative number of individual items contained within the order.
    /// </summary>
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Indicates if the order has a specific scheduled time for delivery or pickup.
    /// </summary>
    /// <value>True if DeliveryTime is set and is not zero; otherwise, false.</value>
    public bool IsScheduled => Order.DeliveryTime.HasValue;

    /// <summary>
    /// Detailed list of products included in the order, including prices and individual quantities.
    /// </summary>
    public IEnumerable<OrderProductDto> Items { get; set; } = new List<OrderProductDto>();
}
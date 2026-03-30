namespace Projeto_SEGUES.Areas.Order.ViewModels;

/// <summary>
/// ViewModel used for the order management sidebar or detailed kanban view.
/// </summary>
/// <remarks>
/// This model encapsulates the logic for order transitions, providing metadata for 
/// status progression (forward/backward) and UI styling (badges).
/// </remarks>
public class OrderManagementSideViewModel
{
    /// <summary>Unique identifier of the order.</summary>
    public int Id { get; set; }

    /// <summary>Total monetary value of the order, pre-formatted as a currency string.</summary>
    public string FormattedTotalValue { get; set; } = string.Empty;

    /// <summary>Total count of all products contained within the order.</summary>
    public int TotalQuantity { get; set; }

    /// <summary>The full name of the user who placed the order.</summary>
    public string BuyerName { get; set; } = string.Empty;

    /// <summary>The ID of the current state of the order in the workflow.</summary>
    public int CurrentStatusId { get; set; }

    /// <summary>The localized or display-friendly name of the current status.</summary>
    public string StatusDisplayName { get; set; } = string.Empty;

    /// <summary>The Bootstrap or CSS class used to render the status badge (e.g., bg-warning, bg-success).</summary>
    public string StatusBadgeClass { get; set; } = string.Empty;

    /// <summary>The ID of the immediate previous status in the workflow.</summary>
    public int PrevStatusId { get; set; }

    /// <summary>Indicates if the order's state can be reverted to the previous status.</summary>
    public bool CanGoBack { get; set; }

    /// <summary>The ID of the next possible status in the workflow.</summary>
    public int NextStatusId { get; set; }

    /// <summary>Indicates if the order is eligible to progress to the next status.</summary>
    public bool CanGoForward { get; set; }

    /// <summary>List of products and quantities included in this specific order.</summary>
    public IEnumerable<OrderProductDto> Items { get; set; } = new List<OrderProductDto>();
}
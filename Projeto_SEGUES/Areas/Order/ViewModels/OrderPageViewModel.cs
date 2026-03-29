namespace Projeto_SEGUES.Areas.Order.ViewModels;

/// <summary>
/// Data contract for the main landing page of the Order Area.
/// </summary>
/// <remarks>
/// This ViewModel aggregates user financial data, current shopping cart status, 
/// and complex business availability logic (Schedules and Weekend toggles) 
/// to inform the user of the service status before they initiate a purchase.
/// </remarks>
public class OrderPageViewModel
{
    /// <summary>The user's current balance formatted as a localized currency string.</summary>
    public string UserBalance { get; set; }
    
    // ==========================================
    // Shopping Cart Summary
    // ==========================================
    
    /// <summary>Contains the calculated totals (quantity and value) of the active cart.</summary>
    public OrderTotalViewModel CartTotal { get; set; } = new ();

    /// <summary>Helper property to retrieve the total number of items in the cart.</summary>
    public int CartTotalQuantity => CartTotal.TotalQuantity;

    /// <summary>Helper property to retrieve the total value of the cart formatted as currency.</summary>
    public string CartTotalValueString => CartTotal.TotalValue.ToString("C");
    
    // ==========================================
    // Service Configuration & Availability
    // ==========================================
    
    /// <summary>Formatted opening time for display (e.g., "08:30").</summary>
    public string BarOpeningTimeString { get; set; } = "";

    /// <summary>Formatted closing time for display (e.g., "20:00").</summary>
    public string BarClosingTimeString { get; set; } = "";

    /// <summary>External or internal URL for the PDF/Digital Bar Menu.</summary>
    public string BarMenuLink { get; set; } = "";

    /// <summary>Flag indicating if the establishment is configured to open on Saturdays.</summary>
    public bool IsOpenSaturday { get; set; }

    /// <summary>Flag indicating if the establishment is configured to open on Sundays.</summary>
    public bool IsOpenSunday { get; set; }

    /// <summary>A descriptive string listing special operational days (e.g., ", Sáb, Dom").</summary>
    public string ExtraDays { get; set; } = "";
    
    // ==========================================
    // Real-time Status Flags
    // ==========================================

    /// <summary>Computed flag: True if the current day is a weekend and the service is disabled.</summary>
    public bool IsClosedByWeekend { get; set; }

    /// <summary>Computed flag: True if the current time falls outside the defined BarOpening/Closing window.</summary>
    public bool IsOutsideHours { get; set; }

    /// <summary>
    /// Master toggle for the UI. Returns true if the service is unavailable for any reason.
    /// Used to disable the "Order Now" button in the View.
    /// </summary>
    public bool IsClosed => IsClosedByWeekend || IsOutsideHours;
}
namespace Projeto_SEGUES.Areas.Order.ViewModels;

public class OrderPageViewModel
{
    public string UserBalance { get; set; }
    
    // Cart
    public OrderTotalViewModel CartTotal { get; set; } = new ();
    public int CartTotalQuantity => CartTotal.TotalQuantity;
    public string CartTotalValueString => CartTotal.TotalValue.ToString("C");
    
    // Config
    public string BarOpeningTimeString { get; set; } = "";
    public string BarClosingTimeString { get; set; } = "";
    public string BarMenuLink { get; set; } = "";
    public bool IsOpenSaturday { get; set; }
    public bool IsOpenSunday { get; set; }
    
    public bool IsClosedByWeekend { get; set; }
    public bool IsOutsideHours { get; set; }
    public bool IsClosed => IsClosedByWeekend || IsOutsideHours;
}
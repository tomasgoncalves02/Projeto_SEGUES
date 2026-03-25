using Microsoft.AspNetCore.Mvc.Rendering;

namespace Projeto_SEGUES.Areas.Order.ViewModels;

public class CreateOrderViewModel
{
    public IEnumerable<OrderProductDto> Products { get; set; } = new List<OrderProductDto>();
    
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    
    public OrderTotalViewModel CartTotal { get; set; } = new();
}
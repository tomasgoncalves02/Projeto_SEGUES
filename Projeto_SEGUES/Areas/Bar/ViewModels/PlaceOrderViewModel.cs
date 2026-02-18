using System.Collections.Generic;

namespace Projeto_SEGUES.Areas.Bar.ViewModels
{
    public class PlaceOrderViewModel
    {
        public List<ProductItemViewModel> AvailableProducts { get; set; } = new();
    }

    public class ProductItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}
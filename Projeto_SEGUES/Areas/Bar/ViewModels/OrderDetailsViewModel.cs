namespace Projeto_SEGUES.Areas.Bar.ViewModels
{
    public class OrderDetailsViewModel
    {
        public int Id { get; set; }
        public string RedemptionCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public TimeSpan OrderPickUp { get; set; }
        public int Status { get; set; }
        public string State => Status switch
        {
            0 => "Pendente",
            1 => "Em Preparação",
            2 => "Entrega Pendente",
            3 => "Entregue",
            _ => "Cancelado"
        };
        public decimal Total { get; set; }
        public List<OrderDetailsItemViewModel> Products { get; set; } = new();
    }

    public class OrderDetailsItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; }
    }
}
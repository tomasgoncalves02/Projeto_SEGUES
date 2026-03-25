using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Report.ViewModels;

public class ReportOrderSearchViewModel
{
    public string? SearchString { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime? DateFilter { get; set; }
    
    public OrderStatus? StatusFilter { get; set; }
    
    public IEnumerable<Models.Order.Order> Results { get; set; } = new List<Models.Order.Order>();
}
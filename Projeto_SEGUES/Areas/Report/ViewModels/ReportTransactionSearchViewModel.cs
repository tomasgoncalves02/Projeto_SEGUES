using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Payment;

namespace Projeto_SEGUES.Areas.Report.ViewModels;

public class ReportTransactionSearchViewModel
{
    public string? SearchString { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime? DateFilter { get; set; }
    
    public string? TypeFilter { get; set; } 
    
    public IEnumerable<Transaction> Results { get; set; } = new List<Transaction>();
}
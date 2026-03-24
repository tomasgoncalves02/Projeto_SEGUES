using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Report.ViewModels;

public class ReportTicketSearchViewModel
{
    [Display(Name = "Pesquisar")]
    public string? SearchString { get; set; }
    
    [Display(Name = "Estado")]
    public TicketState? StateFilter { get; set; }

    [Display(Name = "Fluxo")]
    public TicketFlow? FlowFilter { get; set; }

    [Display(Name = "A partir de")]
    [DataType(DataType.Date)]
    public DateTime? DateFilter { get; set; }

    // Hold the results returned from the database
    public IEnumerable<Models.Ticket.Ticket> Results { get; set; } = new List<Models.Ticket.Ticket>();
}
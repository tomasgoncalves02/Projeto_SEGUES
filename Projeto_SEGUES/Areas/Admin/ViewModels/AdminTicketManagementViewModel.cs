using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Ticket;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

public class AdminTicketManagementViewModel
{
    // Schedule
    public string LunchOpeningTime { get; set; } = "";
    public string LunchClosingTime { get; set; } = "";
    public string DinnerOpeningTime { get; set; } = "";
    public string DinnerClosingTime { get; set; } = "";
    
    // Tickets prices and validity days
    public List<TicketPrice> Prices { get; set; } = [];
    public int CurrentValidityDays { get; set; }

    // Search model
    public ReportTicketSearchViewModel SearchModel { get; set; } = new();
}
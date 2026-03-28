using Projeto_SEGUES.Areas.Report.ViewModels;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

public class AdminOrderManagementViewModel
{
    public string BarOpeningTimeString { get; set; } = "";
    public string BarClosingTimeString { get; set; } = "";
    
    public ReportOrderSearchViewModel SearchModel { get; set; } = new ();
}
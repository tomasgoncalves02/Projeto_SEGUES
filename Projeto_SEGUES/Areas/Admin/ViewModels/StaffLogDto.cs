namespace Projeto_SEGUES.Areas.Admin.ViewModels;

public class StaffLogDto
{
    public string EmployeeName { get; set; } = "";
    public string EmployeeEmail { get; set; } = "";
    public string DateDisplay { get; set; } = "";
    public string TimeDisplay { get; set; } = "";
    public string UserAction { get; set; } = "";
    public string FullMessage { get; set; } = "";
    public string RequestPath { get; set; } = "";
}
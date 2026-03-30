namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// Data Transfer Object (DTO) for StaffLog models.
/// </summary>
public class StaffLogDto
{
    /// <summary>
    /// Employee Name associated with the log entry.
    /// </summary>
    public string EmployeeName { get; set; } = "";
    
    /// <summary>
    /// Employee Email associated with the log entry.
    /// </summary>
    public string EmployeeEmail { get; set; } = "";
    
    /// <summary>
    /// Date of the log entry.
    /// </summary>
    public string DateDisplay { get; set; } = "";
    
    /// <summary>
    /// Time of the log entry.
    /// </summary>
    public string TimeDisplay { get; set; } = "";
    
    /// <summary>
    /// User Action associated with the log entry.
    /// </summary>
    public string UserAction { get; set; } = "";
    
    /// <summary>
    /// Full Message associated with the log entry.
    /// </summary>
    public string FullMessage { get; set; } = "";
    
    /// <summary>
    /// Request Path associated with the log entry.
    /// </summary>
    public string RequestPath { get; set; } = "";
}
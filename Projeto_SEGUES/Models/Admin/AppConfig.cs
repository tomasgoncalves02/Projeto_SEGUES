using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Admin;

public class AppConfig
{
    public int Id { get; init; }

    [Required]
    public int MaxTicketsPerUser { get; set; } = 100;
    
    [Required]
    public int TicketValidityDays { get; set; } = 365; // Default: 1 year

    [Required]
    public TimeSpan OpenBarTime { get; set; } = new(8, 30, 0);

    [Required]
    public TimeSpan CloseBarTime { get; set; } = new(23, 50, 0);
    [Required]
    public TimeSpan OpenLunchTime { get; set; } = new(12, 0, 0);

    [Required]
    public TimeSpan CloseLunchTime { get; set; } = new(14, 30, 0);

    [Required]
    public TimeSpan OpenDinnerTime { get; set; } = new(19, 0, 0);

    [Required]
    public TimeSpan CloseDinnerTime { get; set; } = new(21, 30, 0);
}
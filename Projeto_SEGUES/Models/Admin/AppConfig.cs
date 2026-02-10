using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Admin;

public class AppConfig
{
    public int Id { get; init; }

    [Required]
    public int MaxTicketsPerUser { get; set; } = 100;
    
    [Required]
    public int TicketValidityDays { get; set; } = 365; // Default: 1 year
}
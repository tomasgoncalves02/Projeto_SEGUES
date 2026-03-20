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
    public TimeSpan BarOpeningTime { get; set; } = new(8, 30, 0);

    [Required]
    public TimeSpan BarClosingTime { get; set; } = new(23, 50, 0);
    [Required]
    public TimeSpan CanteenLunchOpeningTime { get; set; } = new(12, 0, 0);

    [Required]
    public TimeSpan CanteenLunchClosingTime { get; set; } = new(14, 30, 0);

    [Required]
    public TimeSpan CanteenDinnerOpeningTime { get; set; } = new(19, 0, 0);

    [Required]
    public TimeSpan CanteenDinnerClosingTime { get; set; } = new(21, 30, 0);

    [Url]
    public string? BarLink { get; set; } = "https://www.ips.pt";

    [Url]
    public string? CanteenLink { get; set; } = "https://www.ips.pt";
}
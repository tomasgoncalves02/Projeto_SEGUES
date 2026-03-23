using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Admin;

public class AppConfig
{
    public int Id { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Número máximo de bilhetes por utilizador deve ser pelo menos 1.")]
    public int MaxTicketsPerUser { get; set; } = 100;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Número de dias de validade dos bilhetes deve ser pelo menos 1.")]
    public int TicketValidityDays { get; set; } = 365; // Default: 1 year

    [Required]
    [Display(Name = "Hora de Abertura do Bar")]
    public TimeSpan BarOpeningTime { get; set; } = new(8, 30, 0);

    [Required]
    [Display(Name = "Hora de Fecho do Bar")]
    public TimeSpan BarClosingTime { get; set; } = new(23, 50, 0);
    
    [Required]
    [Display(Name = "Hora de Abertura do Almoço noRefeitório")]
    public TimeSpan CanteenLunchOpeningTime { get; set; } = new(12, 0, 0);

    [Required]
    [Display(Name = "Hora de Fecho do Almoço no Refeitório")]
    public TimeSpan CanteenLunchClosingTime { get; set; } = new(14, 30, 0);

    [Required]
    [Display(Name = "Hora de Abertura do Jantar no Refeitório")]
    public TimeSpan CanteenDinnerOpeningTime { get; set; } = new(19, 0, 0);

    [Required]
    [Display(Name = "Hora de Fecho do Jantar no Refeitório")]
    public TimeSpan CanteenDinnerClosingTime { get; set; } = new(21, 30, 0);

    [Url]
    [Display(Name = "Link do Bar")]
    public string BarLink { get; set; } = "https://www.ips.pt";

    [Url]
    [Display(Name = "Link do Refeitório")]
    public string CanteenLink { get; set; } = "https://www.ips.pt";
}
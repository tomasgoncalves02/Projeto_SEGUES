using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Admin;

/// <summary>
/// Entity representing the global system configuration and business rules.
/// </summary>
/// <remarks>
/// This entity stores the fundamental parameters for the SEGUES platform, 
/// including ticket limits, validity periods, and operational hours for the Bar and Canteen.
/// </remarks>
public class AppConfig
{
    /// <summary>Unique identifier for the configuration record.</summary>
    public int Id { get; init; }

    /// <summary>Maximum number of active tickets a single user is allowed to hold at once.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Número máximo de bilhetes por utilizador deve ser pelo menos 1.")]
    public int MaxTicketsPerUser { get; set; } = 100;

    /// <summary>The duration (in days) before a purchased ticket is marked as expired.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Número de dias de validade dos bilhetes deve ser pelo menos 1.")]
    public int TicketValidityDays { get; set; } = 365; // Default: 1 year

    /// <summary>Opening time for the Bar facility.</summary>
    [Required]
    [Display(Name = "Hora de Abertura do Bar")]
    public TimeSpan BarOpeningTime { get; set; } = new(8, 30, 0);

    /// <summary>Closing time for the Bar facility.</summary>
    [Required]
    [Display(Name = "Hora de Fecho do Bar")]
    public TimeSpan BarClosingTime { get; set; } = new(23, 50, 0);

    /// <summary>Opening time for the Canteen's lunch service.</summary>
    [Required]
    [Display(Name = "Hora de Abertura do Almoço no Refeitório")]
    public TimeSpan CanteenLunchOpeningTime { get; set; } = new(12, 0, 0);

    /// <summary>Closing time for the Canteen's lunch service.</summary>
    [Required]
    [Display(Name = "Hora de Fecho do Almoço no Refeitório")]
    public TimeSpan CanteenLunchClosingTime { get; set; } = new(14, 30, 0);

    /// <summary>Opening time for the Canteen's dinner service.</summary>
    [Required]
    [Display(Name = "Hora de Abertura do Jantar no Refeitório")]
    public TimeSpan CanteenDinnerOpeningTime { get; set; } = new(19, 0, 0);

    /// <summary>Closing time for the Canteen's dinner service.</summary>
    [Required]
    [Display(Name = "Hora de Fecho do Jantar no Refeitório")]
    public TimeSpan CanteenDinnerClosingTime { get; set; } = new(21, 30, 0);

    /// <summary>External URL for the Bar's digital menu or information page.</summary>
    [Url]
    [Display(Name = "Link do Bar")]
    public string BarLink { get; set; } = "https://www.ips.pt";

    /// <summary>External URL for the Canteen's digital menu or information page.</summary>
    [Url]
    [Display(Name = "Link do Refeitório")]
    public string CanteenLink { get; set; } = "https://www.ips.pt";

    /// <summary>Global flag determining if services are active on Saturdays.</summary>
    public bool IsOpenSaturday { get; set; }

    /// <summary>Global flag determining if services are active on Sundays.</summary>
    public bool IsOpenSunday { get; set; }
}
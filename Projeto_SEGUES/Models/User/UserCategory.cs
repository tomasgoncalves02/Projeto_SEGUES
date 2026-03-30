using Projeto_SEGUES.Models.Ticket;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_SEGUES.Models.User;

/// <summary>
/// Entity representing a classification group for users to determine pricing and permissions.
/// </summary>
/// <remarks>
/// This model acts as the primary key for the <see cref="TicketPrice"/> engine. 
/// It allows for granular control over meal costs based on the user's institutional 
/// status or social support tier.
/// </remarks>
public class UserCategory
{
    /// <summary>Unique identifier for the user category.</summary>
    public int Id { get; init; }

    /// <summary>The display name of the category (e.g., "Escalão B", "Externo").</summary>
    [Required]
    [MaxLength(50, ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
    [Display(Name = "Nome")]
    public required string Name { get; set; }

    /// <summary>Toggle to enable or disable the category for new user assignments.</summary>
    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;

    /// <summary>Historical and current price points associated with this category.</summary>
    public ICollection<TicketPrice> TicketPrices { get; set; } = new List<TicketPrice>();

    /// <summary>
    /// Calculated property that retrieves the most recent price record based on the start date.
    /// </summary>
    /// <remarks>
    /// This property is marked with <see cref="NotMappedAttribute"/> as it is a 
    /// logic-based helper and not a physical database column.
    /// </remarks>
    [NotMapped]
    public TicketPrice LatestPrice => TicketPrices.OrderByDescending(x => x.InitialDatePrice).FirstOrDefault()!;
}
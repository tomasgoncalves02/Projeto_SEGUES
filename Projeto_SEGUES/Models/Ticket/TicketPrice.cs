using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_SEGUES.Models.Ticket;

/// <summary>
/// Entity defining the cost of a meal ticket based on user demographics and time periods.
/// </summary>
/// <remarks>
/// This model implements "Role-Based Pricing." It allows the system to look up the 
/// correct price for a user by matching their <see cref="UserCategory"/> and ensuring 
/// the current date falls within the <see cref="InitialDatePrice"/> and <see cref="EndDatePrice"/> range.
/// </remarks>
public class TicketPrice
{
    /// <summary>Unique identifier for the price rule.</summary>
    public int Id { get; set; }

    /// <summary>The user group this price applies to (e.g., "Aluno Escalão A", "Funcionário").</summary>
    [Required]
    public required UserCategory UserCategory { get; set; } // FK

    /// <summary>The monetary value assigned to the ticket for this specific category.</summary>
    [Required]
    [Range(0, 100, ErrorMessage = "O preço deve ser entre 0 e 100.")]
    [Display(Name = "Preço")]
    [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
    public decimal Price { get; set; }

    /// <summary>The date when this pricing rule becomes effective.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Início")]
    public DateTime InitialDatePrice { get; set; } = DateTime.Now;

    /// <summary>
    /// The date when this pricing rule expires. 
    /// If null, the price is considered the current active rate until a new rule is created.
    /// </summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Fim")]
    public DateTime? EndDatePrice { get; set; } = null;
}
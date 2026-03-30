using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Payment;

/// <summary>
/// Entity representing a raw financial transaction or payment attempt within the system.
/// </summary>
/// <remarks>
/// This model serves as the primary ledger for external payment integration. It tracks 
/// the payment status (<see cref="IsPaid"/>) and provides a unique <see cref="Reference"/> 
/// for reconciliation with bank statements or payment providers.
/// </remarks>
public class Transaction
{
    /// <summary>Unique identifier for the transaction record.</summary>
    public int Id { get; init; }

    /// <summary>Navigation property to the user who initiated the payment.</summary>
    [Required]
    public required AppUser User { get; init; }

    /// <summary>The total monetary value of the transaction.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    [Display(Name = "Valor da Transação")]
    [DataType(DataType.Currency)]
    [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
    public decimal Amount { get; init; }

    /// <summary>
    /// A unique 8-character alphanumeric string used for tracking and reconciliation.
    /// Default: A truncated GUID in uppercase.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Display(Name = "Refêrencia da Transação")]
    public string Reference { get; init; } = Guid.NewGuid().ToString()[..8].ToUpper();

    /// <summary>Flag indicating if the payment has been confirmed by the processing provider.</summary>
    public bool IsPaid { get; set; }

    /// <summary>The exact timestamp when the transaction record was created.</summary>
    [Display(Name = "Data da Transação")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>Optional notes or details regarding the purpose of the payment.</summary>
    [MaxLength(100, ErrorMessage = "O campo {0} deve conter no máximo {1} caracteres.")]
    [Display(Name = "Descrição da Transação")]
    public string? Description { get; init; }
}
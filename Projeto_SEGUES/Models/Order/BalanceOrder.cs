using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Order;

/// <summary>
/// Entity representing a financial transaction for recharging a user's account balance.
/// </summary>
/// <remarks>
/// This model records the historical data of balance top-ups, which is distinct from 
/// product orders. It serves as the source of truth for financial auditing and 
/// balance reconciliation within the user's digital wallet.
/// </remarks>
public class BalanceOrder
{
    /// <summary>Unique identifier for the balance recharge transaction.</summary>
    public int Id { get; set; }

    /// <summary>The exact date and time when the funds were successfully added to the account.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Transação")]
    public DateTime TransactionDate { get; set; }

    /// <summary>The monetary amount added during this specific transaction.</summary>
    [Range(0, double.MaxValue)]
    [Display(Name = "Valor")]
    [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
    public decimal Value { get; set; }

    /// <summary>Navigation property to the user who received the balance increase.</summary>
    public required User.AppUser AppUser { get; set; } // FK
}
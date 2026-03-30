using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Defines the operational status and validity of a digital ticket.
/// </summary>
/// <remarks>
/// This enum is used to control the ticket validation process. Only tickets in the 
/// <see cref="Available"/> state can be scanned or transferred.
/// Inherits from <see cref="byte"/> to optimize the <c>Ticket</c> table storage.
/// </remarks>
public enum TicketState : byte
{
    /// <summary>The ticket is valid, active, and ready to be used or transferred.</summary>
    [Display(Name = "Disponível")]
    Available,

    /// <summary>The ticket has already been scanned and redeemed at a collection point.</summary>
    [Display(Name = "Utilizada")]
    Used,

    /// <summary>The ticket has passed its validity period (defined in AppConfig) without being used.</summary>
    [Display(Name = "Expirada")]
    Expired
}
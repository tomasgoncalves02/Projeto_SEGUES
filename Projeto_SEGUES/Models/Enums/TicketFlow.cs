using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Represents the origin or destination flow of a ticket within the user's history.
/// </summary>
/// <remarks>
/// This enum is primarily used in the <c>TicketReportViewModel</c> to filter how 
/// a ticket entered or left a user's digital wallet (Direct Purchase vs. P2P Transfer).
/// Inherits from <see cref="byte"/> to maintain a low memory footprint.
/// </remarks>
public enum TicketFlow : byte
{
    /// <summary>Tickets acquired directly through the shop's checkout process.</summary>
    [Display(Name = "Compradas")]
    Bought,

    /// <summary>Tickets that the user transferred out to another student or employee.</summary>
    [Display(Name = "Enviadas")]
    Sent,

    /// <summary>Tickets that were transferred into the user's account by someone else.</summary>
    [Display(Name = "Recebidas")]
    Received
}
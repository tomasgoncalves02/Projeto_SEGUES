using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Represents the origin/destination flow of a ticket within the system.
/// </summary>
public enum TicketFlow : byte
{
    [Display(Name = "Todas")]
    All = 0,

    [Display(Name = "Compradas")]
    Bought = 1,

    [Display(Name = "Enviadas")]
    Sent = 2,

    [Display(Name = "Recebidas")]
    Received = 3
}
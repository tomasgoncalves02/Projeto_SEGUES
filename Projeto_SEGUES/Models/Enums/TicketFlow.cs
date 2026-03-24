using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Represents the origin/destination flow of a ticket within the system.
/// </summary>
public enum TicketFlow : byte
{
    [Display(Name = "Todas")]
    All,

    [Display(Name = "Compradas")]
    Bought,

    [Display(Name = "Enviadas")]
    Sent,

    [Display(Name = "Recebidas")]
    Received
}
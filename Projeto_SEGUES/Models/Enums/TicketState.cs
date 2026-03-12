using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

public enum TicketState : byte
{
    [Display(Name = "Disponível")]
    Available,
    [Display(Name = "Utilizada")]
    Used,
    [Display(Name = "Expirada")]
    Expired
}
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

public enum TicketState
{
    [Display(Name = "Disponível")]
    Available,
    [Display(Name = "Utilizada")]
    Used,
    [Display(Name = "Expirada")]
    Expired
}
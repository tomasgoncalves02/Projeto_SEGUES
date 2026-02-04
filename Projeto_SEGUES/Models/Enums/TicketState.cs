using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

public enum TicketState
{
    [Display(Name = "Disponível")]
    Available,
    [Display(Name = "Usado")]
    Used,
    [Display(Name = "Expirado")]
    Expired
}
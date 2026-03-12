using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

public enum UserStatus : byte
{
    [Display(Name = "Activo")]
    Active,
    [Display(Name = "Inactivo")]
    Inactive,
    [Display(Name = "Suspenso")]
    Suspended
}

using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

public enum Gender : byte
{
    [Display(Name = "Masculino")]
    Male,
    [Display(Name = "Feminino")]
    Female,
    [Display(Name = "Outro")]
    Other
}
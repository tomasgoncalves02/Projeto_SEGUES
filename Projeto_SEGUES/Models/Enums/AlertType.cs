using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

public enum AlertType
{
    [Display(Name = "Sucesso")]
    Success,
    [Display(Name = "Erro")]
    Error,
    [Display(Name = "Aviso")]
    Warning,
    [Display(Name = "Informação")]
    Info,
    [Display(Name = "Confirmação")]
    Confirm
}
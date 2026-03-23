using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit.ViewModels;

public class ErrorViewModel
{
    [Display(Name = "ID do Pedido")]
    public string? RequestId { get; set; }
    
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    
    [Required]
    [Display(Name = "Mensagem de Erro")]
    public required string ErrorMessage { get; set; }
    
    [Required]
    [Display(Name = "Código de Erro")]
    public required int ErrorCode { get; set; }
}
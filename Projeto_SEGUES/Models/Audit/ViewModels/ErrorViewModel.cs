using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Models.Audit.ViewModels
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        
        public string ErrorMessage { get; set; }
        
        public AppErrors ErrorCode { get; set; }
    }
}

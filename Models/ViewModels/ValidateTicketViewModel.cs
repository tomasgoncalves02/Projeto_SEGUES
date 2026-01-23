using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.ViewModels
{
    public class ValidateTicketViewModel
    {
        
        [Required(ErrorMessage = "Introduza o código da senha.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "O código deve ter exatamente 8 caracteres.")]
        [Display(Name = "Código da Senha")]
        public string Code { get; set; }

       
        public List<Ticket> RecentTickets { get; set; } = new List<Ticket>();
    }
}

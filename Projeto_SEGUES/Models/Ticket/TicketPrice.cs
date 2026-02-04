using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Models.Ticket
{
    public class TicketPrice
    {
        public int Id { get; set; }
        
        [Required]
        public required UserCategory UserCategory { get; set; } // FK
        
        [Required]
        [Range(0, 100)]
        [Display(Name = "Preço")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Price { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Início")]
        public DateTime InitialDatePrice { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Fim")]
        public DateTime EndDatePrice { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Models.Ticket
{
    public class Ticket
    {
        public int Id { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Validade")]
        public DateTime ExpirationDate { get; set; }
        
        [Display(Name = "Estado")]
        public TicketState State { get; set; } = TicketState.Available;
        
        [Display(Name = "Usado")]
        public bool IsUsed { get; set; } = false;
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Uso")]
        public DateTime? UsedDate { get; set; }
        
        [Required]
        public required User.User Owner { get; set; } // FK
        
        [MaxLength(8)]
        [Display(Name = "Código de Validação")]
        public string ValidationCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        
        public required TicketPurchase TicketPurchase { get; set; } // FK
        public ICollection<TicketTransfer> Transfers { get; set; } = new List<TicketTransfer>();
    }
}

using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Ticket
{
    public class TicketPurchase
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Quantidade")]
        public int Quantity { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data da Transação")]
        public DateTime TransactionDate { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Valor")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal Value { get; set; }

        [Required]
        public required AppUser AppUser { get; set; } // FK

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}

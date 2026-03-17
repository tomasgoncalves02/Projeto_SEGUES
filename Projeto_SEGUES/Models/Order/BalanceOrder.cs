using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Order
{
    public class BalanceOrder
    {
        public int Id { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Transação")]
        public DateTime TransactionDate { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Valor")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal Value { get; set; }

        public required User.AppUser AppUser { get; set; } // FK
    }
}

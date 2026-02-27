using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Models.Payment
{
    public class Transaction
    {
        public int Id { get; init; }
        
        [Required]
        public required AppUser User { get; init; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        [Display(Name = "Valor da Transação")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; init; }
        
        [Required]
        [MaxLength(15)]
        [Display(Name = "Número de Telemóvel")]
        [DataType(DataType.PhoneNumber)]
        public required string PhoneNumber { get; init; }
        
        [Required]
        [MaxLength(100)]
        [Display(Name = "Refêrencia da Transação")]
        public required string Reference { get; init; }
        
        public bool IsPaid { get; set; } = false;
        
        [Display(Name = "Data da Transação")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime CreatedAt { get; init; } = DateTime.Now;

        [MaxLength(100)]
        public string? Description { get; init; }
    }
}
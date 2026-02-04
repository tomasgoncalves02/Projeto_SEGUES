using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit
{
    public class ErrorLog
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        [Display(Name = "Tabela")]
        public required string Table { get; set; }
        
        [Required]
        [MaxLength(100)]
        [Display(Name = "Operação")]
        public required string Operation { get; set; }
        
        [Required]
        [MaxLength(250)]
        [Display(Name = "Mensagem")]
        public required string Message { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data")]
        public DateTime Date { get; set; }
        
        public required User.User User { get; set; } // FK
    }
}

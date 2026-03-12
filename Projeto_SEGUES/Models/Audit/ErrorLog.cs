using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Models.Audit
{
    public class ErrorLog
    {
        public int Id { get; set; }
        
        // Serilog enum levels: Verbose = 0, Debug = 1, Information = 2, Warning = 3, Error = 4, Fatal = 5
        public byte Level { get; set; }
        
        [Required]
        [Display(Name = "Tabela")]
        public required TableName Table { get; set; }
        
        [Required]
        [Display(Name = "Operação")]
        public required AppOperation Operation { get; set; }
        
        [MaxLength(250)]
        [Display(Name = "Origem do pedido")]
        public string? RequestPath { get; set; }
        
        [Required]
        [MaxLength(250)]
        [Display(Name = "Mensagem")]
        public required string Message { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data")]
        public DateTime TimeStamp { get; set; }
        
        public required AppUser AppUser { get; set; } // FK
    }
}

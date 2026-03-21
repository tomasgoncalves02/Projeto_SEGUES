using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit
{
    public class ErrorLog
    {
        public int Id { get; set; }

        // Serilog enum levels: Verbose = 0, Debug = 1, Information = 2, Warning = 3, Error = 4, Fatal = 5
        public byte Level { get; set; }
        
        [Display(Name = "Tabela")]
        public TableName? DbTable { get; set; }
        
        [Display(Name = "Operação")]
        public AppOperation? Operation { get; set; }

        [MaxLength(250)]
        [Display(Name = "Origem do pedido")]
        public string? RequestPath { get; set; }

        [Required]
        [Display(Name = "Mensagem")]
        public required string Message { get; set; }
        
        [Display(Name = "Exceção")]
        public string? Exception { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data")]
        public DateTime TimeStamp { get; set; }

        public string? AppUserId { get; set; }
        
        public AppUser? AppUser { get; set; } // FK
    }
}

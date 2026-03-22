using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit
{
    public class UserLog
    {
        public int Id { get; init; }
        
        public byte Level { get; set; }
        
        [Display(Name = "Acção")]
        public UserAction? UserAction { get; set; }

        [Required]
        [MaxLength(250)]
        [Display(Name = "Mensagem")]
        public required string Message { get; set; }

        [MaxLength(250)]
        [Display(Name = "Origem do pedido")]
        public string? RequestPath { get; set; }
        
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data")]
        public DateTime TimeStamp { get; set; }

        public string? AppUserId { get; set; }
        
        public AppUser? AppUser { get; set; } // FK
    }
}

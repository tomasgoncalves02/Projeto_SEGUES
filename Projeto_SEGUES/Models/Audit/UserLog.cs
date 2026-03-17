using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit
{
    public class UserLog
    {
        public int Id { get; init; }

        [Required]
        [Display(Name = "Acção")]
        public required UserAction UserAction { get; set; }

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

        public required AppUser AppUser { get; set; } // FK
    }
}

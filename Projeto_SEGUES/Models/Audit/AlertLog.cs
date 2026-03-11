using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Models.Audit
{
    public class AlertLog
    {
        public int Id { get; set; }
        
        [Required]
        public required AlertType AlertType { get; set; }
        
        [Required]
        [MaxLength(250)]
        [Display(Name = "Mensagem")]
        public required string Message { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data")]
        public DateTime Date { get; set; }
        
        public required User.AppUser AppUser { get; set; } // FK
        
        public bool IsRead { get; set; } = false;
    }
}

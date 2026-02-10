using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit
{
    public class UserLog
    {
        public int Id { get; init; }
        
        [MaxLength(100)]
        [Required]
        [Display(Name = "Acção")]
        public required string UserAction { get; set; }
        
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data")]
        public DateTime Date { get; set; }
        
        public required User.AppUser AppUser { get; set; } // FK
    }
}

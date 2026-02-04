using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit
{
    public class DbStats
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        [Display(Name = "Tabela")]
        public required string Table { get; set; }
        
        [Range(0, int.MaxValue)]
        [Display(Name = "Linhas")]
        public int RowsNumb { get; set; }
        
        [Range(0, double.MaxValue)]
        [Display(Name = "Espaço (Kb)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal SpaceKb { get; set; }
        
        [Range(0, double.MaxValue)]
        [Display(Name = "Espaço Reservado (Kb)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal SpaceReserved { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data")]
        public DateTime Date { get; set; } = DateTime.Now;
    }
}

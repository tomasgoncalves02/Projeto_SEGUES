using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit;

/// <summary>
/// Entity used to store historical snapshots of database table statistics.
/// </summary>
/// <remarks>
/// This model allows administrators to monitor table growth, row counts, and disk space 
/// consumption, facilitating proactive database maintenance and capacity planning.
/// </remarks>
public class DbStats
{
    /// <summary>Unique identifier for the statistic record.</summary>
    public int Id { get; set; }

    /// <summary>The name of the database table being measured.</summary>
    [Required]
    [MaxLength(100)]
    [Display(Name = "Tabela")]
    public required string DbTable { get; set; }

    /// <summary>The total number of rows currently stored in the table.</summary>
    [Range(0, int.MaxValue)]
    [Display(Name = "Linhas")]
    public int RowsNumb { get; set; }

    /// <summary>The actual data space occupied by the table in Kilobytes.</summary>
    [Range(0, double.MaxValue)]
    [Display(Name = "Espaço (Kb)")]
    [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
    public decimal SpaceKb { get; set; }

    /// <summary>Total space allocated by the database engine for this table, including overhead.</summary>
    [Range(0, double.MaxValue)]
    [Display(Name = "Espaço Reservado (Kb)")]
    [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
    public decimal SpaceReserved { get; set; }

    /// <summary>The timestamp when the statistics were captured.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data")]
    public DateTime Date { get; set; } = DateTime.Now;
}
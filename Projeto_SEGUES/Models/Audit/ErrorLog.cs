using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit;

/// <summary>
/// Persistence entity for storing structured application errors and exceptions.
/// </summary>
/// <remarks>
/// This model mirrors the properties pushed by the <c>LoggerExtensions</c> and 
/// <c>GlobalExceptionMiddleware</c>, providing a searchable database of system failures.
/// </remarks>
public class ErrorLog
{
    /// <summary>Unique identifier for the error log entry.</summary>
    public int Id { get; init; }

    /// <summary>
    /// Numeric representation of the Serilog severity level.
    /// 0=Verbose, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Fatal.
    /// </summary>
    public byte Level { get; init; }

    /// <summary>The database table context where the error occurred.</summary>
    [Display(Name = "Tabela")]
    public TableName? DbTable { get; init; }

    /// <summary>The type of application operation being performed at the time of failure.</summary>
    [Display(Name = "Operação")]
    public AppOperation? Operation { get; init; }

    /// <summary>The URL or API endpoint that triggered the error.</summary>
    [MaxLength(250)]
    [Display(Name = "Origem do pedido")]
    public string? RequestPath { get; init; }

    /// <summary>The summarized error message or localized error description.</summary>
    [Required]
    [Display(Name = "Mensagem")]
    public required string Message { get; init; }

    /// <summary>The full technical stack trace and exception details for debugging.</summary>
    [Display(Name = "Exceção")]
    public string? Exception { get; init; }

    /// <summary>The exact date and time the error was recorded.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data")]
    public DateTime TimeStamp { get; init; }

    /// <summary>Foreign key identifier for the user who encountered the error.</summary>
    [MaxLength(450)]
    public string? AppUserId { get; init; }

    /// <summary>Navigation property to the user record for detailed audit reporting.</summary>
    public AppUser? AppUser { get; init; } // FK
}
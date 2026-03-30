using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Audit;

/// <summary>
/// Entity used to persist successful user activities and significant business events.
/// </summary>
/// <remarks>
/// This model provides the data structure for the non-error audit trail, 
/// allowing administrators to reconstruct user behavior and verify transaction history.
/// </remarks>
public class UserLog
{
    /// <summary>Unique identifier for the activity log entry.</summary>
    public int Id { get; init; }

    /// <summary>
    /// Severity level of the log entry (typically 'Information' for standard activity).
    /// </summary>
    public byte Level { get; init; }

    /// <summary>The specific type of business action performed (e.g., Login, Purchase, Transfer).</summary>
    [Display(Name = "Acção")]
    public UserAction? UserAction { get; init; }

    /// <summary>A descriptive, human-readable summary of the activity.</summary>
    [Required]
    [MaxLength(250)]
    [Display(Name = "Mensagem")]
    public required string Message { get; init; }

    /// <summary>The URL or endpoint where the user initiated the action.</summary>
    [MaxLength(250)]
    [Display(Name = "Origem do pedido")]
    public string? RequestPath { get; init; }

    /// <summary>The exact date and time the activity occurred.</summary>
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data")]
    public DateTime TimeStamp { get; init; }

    /// <summary>Foreign key identifier for the user who performed the action.</summary>
    [MaxLength(450)]
    public string? AppUserId { get; init; }

    /// <summary>Navigation property to the associated User record.</summary>
    public AppUser? AppUser { get; init; } // FK
}
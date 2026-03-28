using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Defines the administrative and operational state of a user account.
/// </summary>
/// <remarks>
/// This enum is used by the <c>AuthorizationHandler</c> to permit or deny 
/// system access. It inherits from <see cref="byte"/> to optimize the <c>AspNetUsers</c> 
/// table storage and indexing.
/// </remarks>
public enum UserStatus : byte
{
    /// <summary>The account is in good standing and has full access to permitted features.</summary>
    [Display(Name = "Activo")]
    Active,

    /// <summary>The account is disabled (Soft Delete), preventing login and transactions.</summary>
    [Display(Name = "Inactivo")]
    Inactive,

    /// <summary>Access is temporarily restricted, often due to disciplinary or security reasons.</summary>
    [Display(Name = "Suspenso")]
    Suspended
}
namespace Projeto_SEGUES.Models.User;

/// <summary>
/// Specialized user entity representing the workers of an institution.
/// </summary>
public class WorkerIps : AppUser
{
    /// <summary>
    /// Navigation property to the school where the worker is currently employed.
    /// </summary>
    public School? School { get; set; } // FK
}
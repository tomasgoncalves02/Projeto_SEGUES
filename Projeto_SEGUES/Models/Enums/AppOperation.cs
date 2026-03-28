namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Categorization of the fundamental operations performed by the application's data layer.
/// </summary>
/// <remarks>
/// This enum is primarily used by the <c>LoggerExtensions</c> to enrich log metadata, 
/// facilitating granular audits of CRUD (Create, Read, Update, Delete) activities.
/// Using a <see cref="byte"/> base type optimizes storage in the Audit database.
/// </remarks>
public enum AppOperation : byte
{
    /// <summary>Insertion of a new record into the database.</summary>
    Create,

    /// <summary>Retrieval or querying of existing data.</summary>
    Read,

    /// <summary>Modification of an existing database record.</summary>
    Update,

    /// <summary>Removal of a record (Physical or Soft Delete).</summary>
    Delete,

    /// <summary>System-level tasks such as Migrations or Seed Data application.</summary>
    DatabaseInitialization,

    /// <summary>Miscellaneous operations that do not fit standard CRUD patterns.</summary>
    Other
}
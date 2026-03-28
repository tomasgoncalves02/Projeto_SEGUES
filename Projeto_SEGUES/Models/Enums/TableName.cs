namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// A comprehensive map of all database entities and logical modules within the SEGUES platform.
/// </summary>
/// <remarks>
/// This enum is used by the <c>LoggerExtensions</c> and the <c>ErrorLog</c> entity to identify 
/// exactly which table or domain was involved in a system event. 
/// Inherits from <see cref="byte"/> for maximum storage efficiency in audit logs.
/// </remarks>
public enum TableName : byte
{
    /// <summary>Represents a global or cross-module event.</summary>
    All,

    // Core Configuration
    AppConfig,

    // Financial & Orders
    BalanceOrder,
    DbStats,
    Discount,

    // Identity & User Sub-types (TPT)
    Employee,
    Identity,

    // Transactional Core
    Order,
    OrderLine,

    // Geography & Infrastructure
    PostalCode,

    // Inventory & Catalog
    Product,
    ProductCategory,

    // Education & Demographic
    School,
    Student,

    // Ticketing Domain
    Ticket,
    TicketPrice,
    TickerPurchase,
    TicketTransfer,

    // Payments & Users
    Transaction,
    User,
    UserCategory,

    // Auditing
    UserLog
}
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
    /// <summary>
    /// Represents the centralized configuration of the application.
    /// </summary>
    AppConfig,

    // Financial & Orders
    /// <summary>
    /// Represents the financial balance and transactions of the system, including user balances and payment records.
    /// </summary>
    BalanceOrder,
    /// <summary>
    /// Represents the statistical data of the database, such as record counts and performance metrics, used for monitoring and optimization.
    /// </summary>
    DbStats,
    /// <summary>
    /// Represents the promotional codes and discounts used in the system, including coupon codes and promo codes.
    /// </summary>
    Discount,

    // Identity & User Sub-types (TPT)
    /// <summary>
    /// Represents the employee data of the system, including their roles and responsibilities.
    /// </summary>
    Employee,
    /// <summary>
    /// Represents the user data of the system, including their personal information and access credentials.
    /// </summary>
    Identity,

    // Transactional Core
    /// <summary>
    /// Represents the order data of the system, including order details, products, and payment information.
    /// </summary>
    Order,
    /// <summary>
    /// Represents the line items of an order, including product quantities and prices.
    /// </summary>
    OrderLine,

    // Geography & Infrastructure
    /// <summary>
    /// Represents the postal codes used in the system, including geographic coordinates and address information.
    /// </summary>
    PostalCode,

    // Inventory & Catalog
    /// <summary>
    /// Represents the product data of the system, including product details, categories, and inventory levels.
    /// </summary>
    Product,
    /// <summary>
    /// Represents the categories of products in the system, such as clothing, electronics, and accessories.
    /// </summary>
    ProductCategory,

    // Education & Demographic
    /// <summary>
    /// Represents the school data of the system, including school details, students, and enrollment information.
    /// </summary>
    School,
    /// <summary>
    /// Represents the students enrolled in a school, including their names, grades, and enrollment dates.
    /// </summary>
    Student,

    // Ticketing Domain
    /// <summary>
    /// Represents the ticket data of the system, including ticket details, prices, and ticket purchases.
    /// </summary>
    Ticket,
    /// <summary>
    /// Represents the prices of tickets in the system, including ticket types and prices.
    /// </summary>
    TicketPrice,
    /// <summary>
    /// Represents the records of ticket purchases in the system, including ticket IDs and purchase dates.
    /// </summary>
    TickerPurchase,
    /// <summary>
    /// Represents the records of ticket transfers between users in the system, including ticket IDs and transfer dates.
    /// </summary>
    TicketTransfer,

    // Payments & Users
    /// <summary>
    /// Represents the payment records of the system, including payment methods, transactions, and payment statuses.
    /// </summary>
    Transaction,
    /// <summary>
    /// Represents the user data of the system, including user details, roles, and account information.
    /// </summary>
    User,
    /// <summary>
    /// Represents the categories of users in the system, such as administrators, students, and employees.
    /// </summary>
    UserCategory,

    // Auditing
    /// <summary>
    /// Represents the audit logs of the system, including user actions, system events, and changes made to data.
    /// </summary>
    UserLog,
    /// <summary>
    /// Represents IPS workers.
    /// </summary>
    WorkerIps
}
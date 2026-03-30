namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Centralized enumeration of application-specific error codes.
/// </summary>
/// <remarks>
/// This enum categorizes failures into HTTP standards, Database issues, Identity/Security, 
/// and Domain-specific business logic errors (Tickets, Inventory, Orders).
/// These codes are mapped to localized strings via the <c>AppErrorsExtensions</c>.
/// </remarks>
public enum AppErrors
{
    // ==========================================
    // Standard HTTP mapped errors
    // ==========================================
    /// <summary>
    /// No error.
    /// </summary>
    None = 0,
    /// <summary>
    /// The request was successful.
    /// </summary>
    Ok = 200,
    /// <summary>
    /// The request was not successful.
    /// </summary>
    BadRequest = 400,
    /// <summary>
    /// The user is not authorized to access the requested resource.
    /// </summary>
    Unauthorized = 401,
    /// <summary>
    /// The user is not authorized to perform the requested action.
    /// </summary>
    Forbidden = 403,
    /// <summary>
    /// The requested resource was not found.
    /// </summary>
    NotFound = 404,
    /// <summary>
    /// The request was not valid.
    /// </summary>
    TooManyRequests = 429,
    /// <summary>
    /// The server encountered an unexpected condition that prevented it from fulfilling the request.
    /// </summary>
    InternalServerError = 500,
    /// <summary>
    /// The server is currently unavailable.
    /// </summary>
    ServiceUnavailable = 503,

    // ==========================================
    // Infrastructure & Database Errors (1000+)
    // ==========================================
    /// <summary>
    /// Database connection errors.
    /// </summary>
    DatabaseConnectionError = 1000,
    /// <summary>
    /// Database query errors.
    /// </summary>
    DatabaseQueryError = 1001,
    /// <summary>
    /// Data not found errors.
    /// </summary>
    DataNotFoundError = 1002,
    /// <summary>
    /// Concurrency errors.
    /// </summary>
    ConcurrencyError = 1003,
    /// <summary>
    /// Database update errors.
    /// </summary>
    DatabaseUpdateError = 1004,

    // ==========================================
    // Identity & Access Management (1100+)
    // ==========================================
    /// <summary>
    /// Authentication and authorization errors.
    /// </summary>
    InvalidCredentials = 1100,
    /// <summary>
    /// User not found errors.
    /// </summary>
    UserNotFound = 1101,
    /// <summary>
    /// Invalid token errors.
    /// </summary>
    InvalidToken = 1102,
    /// <summary>
    /// Unauthorized access errors.
    /// </summary>
    UnauthorizedAccess = 1103,
    /// <summary>
    /// Email sending errors.
    /// </summary>
    ResendEmailError = 1104,
    /// <summary>
    /// Email sending errors.
    /// </summary>
    SendActivationEmailError = 1105,

    // ==========================================
    // General Business Logic (1200+)
    // ==========================================
    /// <summary>
    /// Invalid operation errors.
    /// </summary>
    InvalidOperation = 1200,
    /// <summary>
    /// Email sending errors.
    /// </summary>
    EmailSenderError = 1201,

    // ==========================================
    // Ticket System Domain (1300+)
    // ==========================================
    /// <summary>
    /// Pricing not available errors.
    /// </summary>
    PricingNotAvailable = 1301,

    // ==========================================
    // Inventory & Catalog Management (1400+)
    // ==========================================
    /// <summary>
    /// Product category not found errors.
    /// </summary>
    ProductCategoryNotFound = 1400,
    /// <summary>
    /// Product creation errors.
    /// </summary>
    ProductCreateError = 1401,
    /// <summary>
    /// Product not found errors.
    /// </summary>
    ProductNotFound = 1402,
    /// <summary>
    /// Product edit errors.
    /// </summary>
    ProductEditError = 1403,
    /// <summary>
    /// Product deletion errors.
    /// </summary>
    ProductDeleteError = 1404,

    // ==========================================
    // Order & Transaction Processing (1500+)
    // ==========================================
    /// <summary>
    /// Order creation errors.
    /// </summary>
    OrderCreationError = 1500,
    /// <summary>
    /// Order processing errors.
    /// </summary>
    OrderProcessingError = 1501,
    /// <summary>
    /// Order cancellation errors.
    /// </summary>
    OrderCancelError = 1502,
    /// <summary>
    /// Order status transition errors.
    /// </summary>
    InvalidStatusTransition = 1503,

    // ==========================================
    // Catch-all
    // ==========================================
    /// <summary>
    /// Catch-all error for unexpected exceptions.
    /// </summary>
    UnexpectedError = 9999
}
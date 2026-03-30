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
    None = 0,
    Ok = 200,
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    TooManyRequests = 429,
    InternalServerError = 500,
    ServiceUnavailable = 503,

    // ==========================================
    // Infrastructure & Database Errors (1000+)
    // ==========================================
    DatabaseConnectionError = 1000,
    DatabaseQueryError = 1001,
    DataNotFoundError = 1002,
    ConcurrencyError = 1003,
    DatabaseUpdateError = 1004,

    // ==========================================
    // Identity & Access Management (1100+)
    // ==========================================
    InvalidCredentials = 1100,
    UserNotFound = 1101,
    InvalidToken = 1102,
    UnauthorizedAccess = 1103,
    ResendEmailError = 1104,
    SendActivationEmailError = 1105,

    // ==========================================
    // General Business Logic (1200+)
    // ==========================================
    InvalidOperation = 1200,
    EmailSenderError = 1201,

    // ==========================================
    // Ticket System Domain (1300+)
    // ==========================================
    PricingNotAvailable = 1301,

    // ==========================================
    // Inventory & Catalog Management (1400+)
    // ==========================================
    ProductCategoryNotFound = 1400,
    ProductCreateError = 1401,
    ProductNotFound = 1402,
    ProductEditError = 1403,
    ProductDeleteError = 1404,

    // ==========================================
    // Order & Transaction Processing (1500+)
    // ==========================================
    OrderCreationError = 1500,
    OrderProcessingError = 1501,
    OrderCancelError = 1502,
    InvalidStatusTransition = 1503,

    // ==========================================
    // Catch-all
    // ==========================================
    UnexpectedError = 9999,
}
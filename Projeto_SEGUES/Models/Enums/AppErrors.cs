namespace Projeto_SEGUES.Models.Enums;

public enum AppErrors
{
    // Standard HTTP errors
    None = 0,
    Ok = 200,
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    TooManyRequests = 429,
    InternalServerError = 500,
    ServiceUnavailable = 503,

    // App errors

    // Database errors
    DatabaseConnectionError = 1000,
    DatabaseQueryError = 1001,
    DataNotFoundError = 1002,
    ConcurrencyError = 1003,
    DatabaseUpdateError = 1004,

    // Identity errors
    InvalidCredentials = 1100,
    UserNotFound = 1101,
    InvalidToken = 1102,
    UnauthorizedAccess = 1103,
    ResendEmailError = 1104,
    SendActivationEmailError = 1105,

    // Business errors
    InvalidOperation = 1200,
    EmailSenderError = 1201,
    
    // Ticket errors
    PricingNotAvailable = 1301,
    
    // Inventory errors
    ProductCategoryNotFound = 1400,
    ProductCreateError = 1401,
    ProductNotFound = 1402,
    ProductEditError = 1403,
    ProductDeleteError = 1404,
    
    // Order errors
    OrderProcessingError = 1501,
    OrderCancelError = 1502,
    InvalidStatusTransition = 1503,
    
    // Generic errors
    UnexpectedError = 9999,
}
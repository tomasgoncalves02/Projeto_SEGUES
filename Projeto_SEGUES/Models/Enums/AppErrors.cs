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
    
    // Identity errors
    InvalidCredentials = 1100,
    UserNotFound = 1102,
    InvalidToken = 1103,
    UnauthorizedAccess = 1104,
    AccountLocked = 1105,
    UserAlreadyRegistered = 1106,
    
    // Business errors
    InvalidOperation = 1200,
    ValidationError = 1201,
    InsufficientFunds = 1202,
    EmailSenderError = 1203,
    
    // Generic errors
    UnexpectedError = 9999,
}
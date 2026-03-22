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
    UserNotFound = 1102,
    InvalidToken = 1103,
    UnauthorizedAccess = 1104,
    AccountLocked = 1105,
    UserAlreadyRegistered = 1106,
    CodeExpired = 1107,
    IncorrectCode = 1108,
    ResendEmailError = 1109,
    EmailAlreadyRegistered = 1110,
    SendActivationEmailError = 1111,

    // Business errors
    InvalidOperation = 1200,
    ValidationError = 1201,
    InsufficientFunds = 1202,
    EmailSenderError = 1203,
    InvalidTimeFormat = 1204,
    
    // Ticket errors
    InvalidQuantity = 1300,
    PricingNotAvailable = 1301,
    TicketNotFound = 1302,
    TicketAlreadyUsed = 1303,
    TicketExpired = 1304,
    TicketNotAvailable = 1305,
    SenderNotFound = 1306,
    RecipientNotFound = 1307,
    TransferToSelf = 1308,
    CategoryMismatch = 1309,
    TicketsNotOwned = 1310,
    
    // Inventory errors
    ProductCategoryNotFound = 1400,
    ProductAlreadyExists = 1401,
    ProductCreateError = 1402,
    ProductNotFound = 1403,
    ProductEditError = 1404,
    ProductDeleteError = 1405,
    
    // Order errors
    CartEmpty = 1500,
    ShedulePast = 1501,
    InvalidPickupTime = 1502,
    BarClosed = 1503,
    InsufficientStock = 1504,
    OrderProcessingError = 1505,
    OrderCannotCancel = 1506,
    OrderCancelError = 1507,
    InvalidStatusTransition = 1508,
    UseCancelFunction = 1509,
    RedemptionCodeRequired = 1510,
    InvalidRedemptionCode = 1511,
    

    // Generic errors
    UnexpectedError = 9999,
}
using Projeto_SEGUES.Models.Enums;
using Serilog.Context;

namespace Projeto_SEGUES.Extensions;

/// <summary>
/// Extension methods for <see cref="ILogger"/> to implement structured and contextual logging across the application.
/// </summary>
/// <remarks>
/// These methods use Serilog's <c>LogContext</c> to inject custom properties into every log entry, 
/// facilitating advanced filtering and auditing in monitoring tools like Seq or ELK.
/// </remarks>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs a system or database-related error with detailed contextual metadata.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="appError">The application error code (converted to a localized log message).</param>
    /// <param name="table">The database table associated with the operation.</param>
    /// <param name="operation">The type of operation being performed (CRUD).</param>
    /// <param name="exception">The optional caught exception for stack trace logging.</param>
    public static void LogAppError(this ILogger logger, AppErrors appError, TableName table, AppOperation operation, Exception? exception = null)
    {
        string message = appError.GetLogErrorMessage();

        // Enrich the log with properties for structured querying
        using (LogContext.PushProperty("LogType", "Error"))
        using (LogContext.PushProperty("DbTable", (byte)table))
        using (LogContext.PushProperty("Operation", (byte)operation))
        {
            if (exception != null)
            {
                logger.LogError(exception, "{LogText:l} (Table: {TableName}, Operation: {OperationName})",
                    message, table.ToString(), operation.ToString());
            }
            else
            {
                // Non-critical issues are logged as Information but still carry error metadata
                logger.LogInformation("{LogText:l} (Table: {TableName}, Operation: {OperationName})",
                    message, table.ToString(), operation.ToString());
            }
        }
    }

    /// <summary>
    /// Logs a user-initiated action for auditing and activity tracking.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">A descriptive message of the action performed.</param>
    /// <param name="action">The specific type of user action (e.g., Login, Transfer, Purchase).</param>
    public static void LogAppUser(this ILogger logger, string? message, UserAction action)
    {
        using (LogContext.PushProperty("LogType", "UserAction"))
        using (LogContext.PushProperty("UserAction", (byte)action))
        {
            logger.LogInformation("{LogText:l} (UserAction: {ActionName})",
                message, action.ToString());
        }
    }
}
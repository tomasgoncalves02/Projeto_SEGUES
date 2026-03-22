using Projeto_SEGUES.Models.Enums;
using Serilog.Context;

namespace Projeto_SEGUES.Extensions;

public static class LoggerExtensions
{
    public static void LogAppError(this ILogger logger, AppErrors appError, TableName table, AppOperation operation, Exception? exception = null)
    {
        string message = appError.GetLogErrorMessage();
        using (LogContext.PushProperty("LogType", "Error"))
        using (LogContext.PushProperty("DbTable", (byte) table))
        using (LogContext.PushProperty("Operation", (byte) operation))
        {
            if (exception != null)
            {
                logger.LogError(exception, "{Message:l} (Table: {TableName}, Operation: {OperationName})", 
                    message, table.ToString(), operation.ToString());
            }
            else
            {
                logger.LogInformation("{Message:l} (Table: {TableName}, Operation: {OperationName})", 
                    message, table.ToString(), operation.ToString());
            }
        }
    }
    
    public static void LogAppUser(this ILogger logger, string? message, UserAction action)
    {
        using (LogContext.PushProperty("LogType", "UserAction"))
        using (LogContext.PushProperty("UserAction", (byte) action))
        {
            logger.LogInformation("{Message:l} (UserAction: {ActionName})", 
                message, action.ToString());
        }
    }
}
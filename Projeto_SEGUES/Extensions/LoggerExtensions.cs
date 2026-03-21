using Projeto_SEGUES.Models.Enums;
using Serilog.Context;

namespace Projeto_SEGUES.Extensions;

public static class LoggerExtensions
{
    public static void LogAppError(this ILogger logger, string? message, TableName table, AppOperation operation)
    {
        using (LogContext.PushProperty("LogType", "Error"))
        using (LogContext.PushProperty("DbTable", (byte) table))
        using (LogContext.PushProperty("Operation", (byte) operation))
        {
            logger.LogError("{Message} (Table: {TableName}, Operation: {OperationName})", 
                message, table.ToString(), operation.ToString());
        }
    }
    
    public static void LogAppUser(this ILogger logger, string? message, UserAction action)
    {
        using (LogContext.PushProperty("LogType", "UserAction"))
        using (LogContext.PushProperty("UserAction", (byte) action))
        {
            logger.LogInformation("{Message} (UserAction: {ActionName})", 
                message, action.ToString());
        }
    }
}
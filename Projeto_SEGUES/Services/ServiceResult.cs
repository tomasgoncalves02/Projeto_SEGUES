namespace Projeto_SEGUES.Services;

public class ServiceResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; }
    public object? Data { get; private set; } 

    public ServiceResult(bool success, string message, object? data = null)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    public static ServiceResult Ok(string message = "Operation successful.", object? data = null)
    {
        return new ServiceResult(true, message, data);
    }

    public static ServiceResult Fail(string message = "Operation failed.", object? data = null)
    {
        return new ServiceResult(false, message, data);
    }
}
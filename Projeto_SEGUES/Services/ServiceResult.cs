namespace Projeto_SEGUES.Services;

public class ServiceResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; }

    public ServiceResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public static ServiceResult Ok(string message = "Operation successful.") => new(true, message);

    public static ServiceResult Fail(string message = "Operation failed.") => new(false, message);
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private set; }

    public ServiceResult(bool success, string message, T? data = default)
        : base(success, message)
    {
        Data = data;
    }

    public static ServiceResult<T> Ok(string message = "Operation successful.", T? data = default) =>
        new(true, message, data);

    public static ServiceResult<T> Fail(string message = "Operation failed.", T? data = default) =>
        new(false, message, data);
}

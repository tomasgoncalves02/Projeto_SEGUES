namespace Projeto_SEGUES.Services;

/// <summary>
/// Represents the result of a service operation.
/// Used to standardize responses from the service layer to the controllers,
/// encapsulating success status and feedback messages.
/// </summary>
public class ServiceResult
{
    /// <summary>Gets a value indicating whether the operation was successful.</summary>
    public bool Success { get; private set; }

    /// <summary>Gets a feedback message about the operation result.</summary>
    public string Message { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceResult"/> class.
    /// </summary>
    /// <param name="success">Status of the operation.</param>
    /// <param name="message">Description of the outcome.</param>
    public ServiceResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    /// <summary>Returns a successful service result.</summary>
    /// <param name="message">Success message (optional).</param>
    public static ServiceResult Ok(string message = "Operation successful.") => new(true, message);

    /// <summary>Returns a failed service result.</summary>
    /// <param name="message">Error message (optional).</param>
    public static ServiceResult Fail(string message = "Operation failed.") => new(false, message);
}

/// <summary>
/// Represents the result of a service operation that returns data.
/// Inherits from <see cref="ServiceResult"/> to include a typed data payload.
/// </summary>
/// <typeparam name="T">The type of data returned by the operation.</typeparam>
public class ServiceResult<T> : ServiceResult
{
    /// <summary>Gets the data payload returned by the service operation.</summary>
    public T? Data { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceResult{T}"/> class.
    /// </summary>
    /// <param name="success">Status of the operation.</param>
    /// <param name="message">Outcome description.</param>
    /// <param name="data">The data to return.</param>
    private ServiceResult(bool success, string message, T? data = default)
        : base(success, message)
    {
        Data = data;
    }

    /// <summary>Returns a successful service result with data.</summary>
    /// <param name="message">Success message.</param>
    /// <param name="data">The payload to return.</param>
    public static ServiceResult<T> Ok(string message = "Operation successful.", T? data = default) =>
        new(true, message, data);

    /// <summary>Returns a failed service result with optional default data.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="data">The data payload (usually null or default).</param>
    public static ServiceResult<T> Fail(string message = "Operation failed.", T? data = default) =>
        new(false, message, data);
}
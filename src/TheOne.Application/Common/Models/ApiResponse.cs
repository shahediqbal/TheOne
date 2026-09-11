namespace TheOne.Application.Common.Models;

/// <summary>
/// Represents a standard API response wrapper used throughout The One API.
/// </summary>
/// <typeparam name="T">
/// Type of response data.
/// </typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Provides a human-readable response message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Contains the response data.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Contains validation or processing errors.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; init; }
        = Array.Empty<string>();


    /// <summary>
    /// Creates a successful API response.
    /// </summary>
    public static ApiResponse<T> SuccessResponse(
        T data,
        string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }


    /// <summary>
    /// Creates a failed API response.
    /// </summary>
    public static ApiResponse<T> FailureResponse(
        string message,
        IEnumerable<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = (IReadOnlyCollection<string>?)errors?.ToList()
                ?? Array.Empty<string>()
        };
    }
}
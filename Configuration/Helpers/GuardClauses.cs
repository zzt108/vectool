// Create guard helper
using Microsoft.Extensions.Logging;

namespace VecTool.Configuration.Helpers;

public static class GuardClauses
{
    public static T ThrowIfNull<T>(this T? value, string paramName, ILogger? logger = null, string? message = null)
    {
        if (value is null)
        {
            var ex = new ArgumentNullException(paramName);
            logger?.LogError(ex, message ?? $"{paramName} is null");
            throw ex;
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentException with logging if string is null/empty/whitespace.
    /// </summary>
    public static string ThrowIfNullOrWhiteSpace(
        this string? value,
        string paramName,
        ILogger? logger = null,
        string? message = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            var ex = new ArgumentException(
                message ?? $"{paramName} is required and cannot be empty.",
                paramName);
            logger?.LogError(ex, message ?? $"{paramName} is null/empty");
            throw ex;
        }
        return value;
    }

    /// <summary>
    /// Throws InvalidOperationException with logging and custom condition.
    /// </summary>
    public static bool ThrowIfInvalid(
        this bool condition,
        string message,
        ILogger? logger = null)
    {
        if (!condition)
        {
            var ex = new InvalidOperationException(message);
            logger?.LogError(ex, message);
            throw ex;
        }
        return true;
    }
}
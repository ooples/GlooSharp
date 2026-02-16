using System;

namespace GlooSharp;

/// <summary>
/// Exception thrown when a native Gloo operation fails.
/// </summary>
/// <remarks>
/// <para><b>For Beginners:</b>
/// When a Gloo operation fails (e.g., network timeout, invalid argument), the native library
/// returns an error code. This exception wraps that error code with a human-readable message
/// so you can diagnose what went wrong.
/// </para>
/// </remarks>
public class GlooException : Exception
{
    /// <summary>
    /// The native Gloo result code that caused this exception.
    /// </summary>
    public GlooResult Result { get; }

    /// <summary>
    /// Creates a new <see cref="GlooException"/> for the given result code.
    /// </summary>
    /// <param name="result">The native error code.</param>
    public GlooException(GlooResult result)
        : base(GetMessage(result))
    {
        Result = result;
    }

    /// <summary>
    /// Creates a new <see cref="GlooException"/> with a custom message.
    /// </summary>
    /// <param name="result">The native error code.</param>
    /// <param name="message">A custom error message.</param>
    public GlooException(GlooResult result, string message)
        : base(message)
    {
        Result = result;
    }

    /// <summary>
    /// Throws a <see cref="GlooException"/> if the result code indicates failure.
    /// </summary>
    /// <param name="result">The integer result code from a native Gloo call.</param>
    internal static void ThrowIfFailed(int result)
    {
        if (result != (int)GlooResult.Success)
        {
            throw new GlooException((GlooResult)result);
        }
    }

    private static string GetMessage(GlooResult result)
    {
        return result switch
        {
            GlooResult.Success => "Operation completed successfully.",
            GlooResult.InvalidArgument => "Gloo error: One or more arguments were invalid.",
            GlooResult.SystemError => "Gloo error: A system-level error occurred.",
            GlooResult.TransportError => "Gloo error: The transport layer encountered an error.",
            GlooResult.Timeout => "Gloo error: The operation timed out.",
            GlooResult.NotInitialized => "Gloo error: The context has not been initialized.",
            GlooResult.InternalError => "Gloo error: An internal error occurred.",
            _ => $"Gloo error: Unknown error code {(int)result}."
        };
    }
}

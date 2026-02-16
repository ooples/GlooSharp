namespace GlooSharp;

/// <summary>
/// Result codes returned by native Gloo operations.
/// </summary>
/// <remarks>
/// <para><b>For Beginners:</b>
/// Every call to the native Gloo library returns one of these codes to indicate
/// whether the operation succeeded or what kind of error occurred.
/// A value of <see cref="Success"/> (0) means everything worked correctly.
/// </para>
/// </remarks>
public enum GlooResult
{
    /// <summary>Operation completed successfully.</summary>
    Success = 0,

    /// <summary>One or more arguments were invalid (null pointer, out-of-range value, etc.).</summary>
    InvalidArgument = 1,

    /// <summary>A system-level error occurred (memory allocation failure, OS error, etc.).</summary>
    SystemError = 2,

    /// <summary>The transport layer encountered an error (TCP connection failure, InfiniBand error, etc.).</summary>
    TransportError = 3,

    /// <summary>The operation timed out before completing.</summary>
    Timeout = 4,

    /// <summary>The Gloo context has not been initialized.</summary>
    NotInitialized = 5,

    /// <summary>An unknown or internal error occurred.</summary>
    InternalError = 6
}

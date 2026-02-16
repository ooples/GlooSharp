namespace GlooSharp;

/// <summary>
/// Data types supported by native Gloo collective operations.
/// </summary>
/// <remarks>
/// <para><b>For Beginners:</b>
/// When performing collective operations (AllReduce, Broadcast, etc.), Gloo needs to know
/// the type of data in the buffer so it can correctly interpret and combine values.
/// These enum values must match the native C shim's type codes.
/// </para>
/// </remarks>
public enum GlooDataType
{
    /// <summary>32-bit single-precision floating point (C# <c>float</c>).</summary>
    Float32 = 0,

    /// <summary>64-bit double-precision floating point (C# <c>double</c>).</summary>
    Float64 = 1,

    /// <summary>32-bit signed integer (C# <c>int</c>).</summary>
    Int32 = 2,

    /// <summary>64-bit signed integer (C# <c>long</c>).</summary>
    Int64 = 3
}

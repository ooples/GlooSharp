namespace GlooSharp;

/// <summary>
/// Reduction operations supported by native Gloo collective operations.
/// </summary>
/// <remarks>
/// <para><b>For Beginners:</b>
/// When multiple processes each have a value and you want to combine them into one result,
/// you choose a reduction operation:
/// - <see cref="Sum"/>: Add all values together (most common for gradient averaging).
/// - <see cref="Product"/>: Multiply all values together.
/// - <see cref="Min"/>: Keep the smallest value.
/// - <see cref="Max"/>: Keep the largest value.
/// </para>
/// </remarks>
public enum GlooReduceOp
{
    /// <summary>Element-wise sum of all inputs.</summary>
    Sum = 0,

    /// <summary>Element-wise product of all inputs.</summary>
    Product = 1,

    /// <summary>Element-wise minimum across all inputs.</summary>
    Min = 2,

    /// <summary>Element-wise maximum across all inputs.</summary>
    Max = 3
}

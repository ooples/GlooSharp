using System;

namespace GlooSharp;

/// <summary>
/// Managed wrapper around a native Gloo context.
/// </summary>
/// <remarks>
/// <para><b>Overview:</b>
/// A <see cref="GlooContext"/> represents one participant in a distributed group.
/// Each process creates its own context with a unique rank and the total world size.
/// The context is the handle passed to all collective operations (AllReduce, Broadcast, etc.).
/// </para>
/// <para><b>For Beginners:</b>
/// Think of this as your "membership card" in a distributed training group.
/// It knows who you are (Rank) and how many members are in the group (WorldSize).
/// You create one at startup and pass it to every collective operation.
/// </para>
/// <para><b>Lifecycle:</b>
/// <code>
/// using var ctx = new GlooContext(rank: 0, worldSize: 4);
/// GlooTransport.CreateTCP(ctx, "localhost", 29500);
/// GlooCollectives.AllReduce(ctx, data, GlooReduceOp.Sum);
/// // ctx.Dispose() is called automatically at end of using block
/// </code>
/// </para>
/// </remarks>
public sealed class GlooContext : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    /// <summary>
    /// This process's rank (0-based ID) in the distributed group.
    /// </summary>
    public int Rank { get; }

    /// <summary>
    /// Total number of processes in the distributed group.
    /// </summary>
    public int WorldSize { get; }

    /// <summary>
    /// Whether this context holds a valid native handle.
    /// Returns <c>false</c> after <see cref="Dispose"/> has been called.
    /// </summary>
    public bool IsValid => _handle != IntPtr.Zero && !_disposed;

    /// <summary>
    /// The native handle. Used internally by <see cref="GlooCollectives"/> and <see cref="GlooTransport"/>.
    /// </summary>
    internal IntPtr Handle
    {
        get
        {
            if (!IsValid)
            {
                throw new ObjectDisposedException(nameof(GlooContext),
                    "The Gloo context has been disposed or was never initialized.");
            }

            return _handle;
        }
    }

    /// <summary>
    /// Creates a new Gloo context for the specified rank and world size.
    /// </summary>
    /// <param name="rank">
    /// This process's rank (0-based). Must be in the range [0, worldSize).
    /// </param>
    /// <param name="worldSize">
    /// Total number of processes participating. Must be at least 1.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rank"/> or <paramref name="worldSize"/> is out of valid range.
    /// </exception>
    /// <exception cref="GlooException">
    /// Thrown if the native context creation fails.
    /// </exception>
    public GlooContext(int rank, int worldSize)
    {
        if (worldSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(worldSize),
                worldSize, "World size must be at least 1.");
        }

        if (rank < 0 || rank >= worldSize)
        {
            throw new ArgumentOutOfRangeException(nameof(rank),
                rank, $"Rank must be between 0 and {worldSize - 1}.");
        }

        Rank = rank;
        WorldSize = worldSize;

        int result = GlooNativeLibrary.gloo_context_create(rank, worldSize, out _handle);
        GlooException.ThrowIfFailed(result);
    }

    /// <summary>
    /// Releases the native Gloo context.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_handle != IntPtr.Zero)
        {
            GlooNativeLibrary.gloo_context_destroy(_handle);
            _handle = IntPtr.Zero;
        }
    }
}

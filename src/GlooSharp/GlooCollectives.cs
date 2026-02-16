using System;
using System.Runtime.InteropServices;

namespace GlooSharp;

/// <summary>
/// Static methods for performing Gloo collective operations via native P/Invoke.
/// </summary>
/// <remarks>
/// <para><b>Overview:</b>
/// Collective operations are communication patterns where all processes in a group participate.
/// These are the building blocks for distributed training: AllReduce for gradient averaging,
/// Broadcast for parameter distribution, AllGather for collecting model shards, etc.
/// </para>
/// <para><b>For Beginners:</b>
/// Each method pins a managed array in memory (so the garbage collector doesn't move it),
/// passes the pointer to the native Gloo library, and then frees the pin.
/// You need a valid <see cref="GlooContext"/> with a transport attached before calling these.
/// </para>
/// <para><b>Usage:</b>
/// <code>
/// using var ctx = new GlooContext(rank: 0, worldSize: 4);
/// GlooTransport.CreateTCP(ctx, "localhost", 29500);
///
/// var data = new float[] { 1.0f, 2.0f, 3.0f };
/// GlooCollectives.AllReduce(ctx, data, GlooReduceOp.Sum);
/// // data now contains the sum from all 4 processes
/// </code>
/// </para>
/// </remarks>
public static class GlooCollectives
{
    // ─── AllReduce ───────────────────────────────────────────────────

    /// <summary>
    /// Performs an in-place AllReduce on a <c>float[]</c> buffer.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="data">The data buffer. Will be overwritten with the reduced result.</param>
    /// <param name="op">The reduction operation to apply.</param>
    public static void AllReduce(GlooContext context, float[] data, GlooReduceOp op)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            var ptr = handle.AddrOfPinnedObject();
            int result = GlooNativeLibrary.gloo_allreduce(
                context.Handle, ptr, ptr, data.Length, (int)GlooDataType.Float32, (int)op);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Performs an in-place AllReduce on a <c>double[]</c> buffer.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="data">The data buffer. Will be overwritten with the reduced result.</param>
    /// <param name="op">The reduction operation to apply.</param>
    public static void AllReduce(GlooContext context, double[] data, GlooReduceOp op)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            var ptr = handle.AddrOfPinnedObject();
            int result = GlooNativeLibrary.gloo_allreduce(
                context.Handle, ptr, ptr, data.Length, (int)GlooDataType.Float64, (int)op);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Performs an in-place AllReduce on an <c>int[]</c> buffer.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="data">The data buffer. Will be overwritten with the reduced result.</param>
    /// <param name="op">The reduction operation to apply.</param>
    public static void AllReduce(GlooContext context, int[] data, GlooReduceOp op)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            var ptr = handle.AddrOfPinnedObject();
            int result = GlooNativeLibrary.gloo_allreduce(
                context.Handle, ptr, ptr, data.Length, (int)GlooDataType.Int32, (int)op);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Performs an in-place AllReduce on a <c>long[]</c> buffer.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="data">The data buffer. Will be overwritten with the reduced result.</param>
    /// <param name="op">The reduction operation to apply.</param>
    public static void AllReduce(GlooContext context, long[] data, GlooReduceOp op)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            var ptr = handle.AddrOfPinnedObject();
            int result = GlooNativeLibrary.gloo_allreduce(
                context.Handle, ptr, ptr, data.Length, (int)GlooDataType.Int64, (int)op);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            handle.Free();
        }
    }

    // ─── Broadcast ───────────────────────────────────────────────────

    /// <summary>
    /// Broadcasts a <c>float[]</c> buffer from the root process to all other processes.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="data">The data buffer. Root provides the data; non-root processes receive it.</param>
    /// <param name="root">The rank of the broadcasting process.</param>
    public static void Broadcast(GlooContext context, float[] data, int root)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_broadcast(
                context.Handle, handle.AddrOfPinnedObject(), data.Length, (int)GlooDataType.Float32, root);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Broadcasts a <c>double[]</c> buffer from the root process to all other processes.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="data">The data buffer. Root provides the data; non-root processes receive it.</param>
    /// <param name="root">The rank of the broadcasting process.</param>
    public static void Broadcast(GlooContext context, double[] data, int root)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_broadcast(
                context.Handle, handle.AddrOfPinnedObject(), data.Length, (int)GlooDataType.Float64, root);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Broadcasts an <c>int[]</c> buffer from the root process to all other processes.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="data">The data buffer. Root provides the data; non-root processes receive it.</param>
    /// <param name="root">The rank of the broadcasting process.</param>
    public static void Broadcast(GlooContext context, int[] data, int root)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_broadcast(
                context.Handle, handle.AddrOfPinnedObject(), data.Length, (int)GlooDataType.Int32, root);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Broadcasts a <c>long[]</c> buffer from the root process to all other processes.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="data">The data buffer. Root provides the data; non-root processes receive it.</param>
    /// <param name="root">The rank of the broadcasting process.</param>
    public static void Broadcast(GlooContext context, long[] data, int root)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_broadcast(
                context.Handle, handle.AddrOfPinnedObject(), data.Length, (int)GlooDataType.Int64, root);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            handle.Free();
        }
    }

    // ─── AllGather ───────────────────────────────────────────────────

    /// <summary>
    /// Gathers <c>float[]</c> data from all processes and returns the concatenated result.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="sendData">This process's contribution.</param>
    /// <returns>Concatenated data from all processes (length = sendData.Length * WorldSize).</returns>
    public static float[] AllGather(GlooContext context, float[] sendData)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (sendData == null) throw new ArgumentNullException(nameof(sendData));

        var recvData = new float[sendData.Length * context.WorldSize];
        var sendHandle = GCHandle.Alloc(sendData, GCHandleType.Pinned);
        var recvHandle = GCHandle.Alloc(recvData, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_allgather(
                context.Handle,
                sendHandle.AddrOfPinnedObject(),
                sendData.Length,
                recvHandle.AddrOfPinnedObject(),
                (int)GlooDataType.Float32);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            sendHandle.Free();
            recvHandle.Free();
        }

        return recvData;
    }

    /// <summary>
    /// Gathers <c>double[]</c> data from all processes and returns the concatenated result.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="sendData">This process's contribution.</param>
    /// <returns>Concatenated data from all processes (length = sendData.Length * WorldSize).</returns>
    public static double[] AllGather(GlooContext context, double[] sendData)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (sendData == null) throw new ArgumentNullException(nameof(sendData));

        var recvData = new double[sendData.Length * context.WorldSize];
        var sendHandle = GCHandle.Alloc(sendData, GCHandleType.Pinned);
        var recvHandle = GCHandle.Alloc(recvData, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_allgather(
                context.Handle,
                sendHandle.AddrOfPinnedObject(),
                sendData.Length,
                recvHandle.AddrOfPinnedObject(),
                (int)GlooDataType.Float64);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            sendHandle.Free();
            recvHandle.Free();
        }

        return recvData;
    }

    /// <summary>
    /// Gathers <c>int[]</c> data from all processes and returns the concatenated result.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="sendData">This process's contribution.</param>
    /// <returns>Concatenated data from all processes (length = sendData.Length * WorldSize).</returns>
    public static int[] AllGather(GlooContext context, int[] sendData)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (sendData == null) throw new ArgumentNullException(nameof(sendData));

        var recvData = new int[sendData.Length * context.WorldSize];
        var sendHandle = GCHandle.Alloc(sendData, GCHandleType.Pinned);
        var recvHandle = GCHandle.Alloc(recvData, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_allgather(
                context.Handle,
                sendHandle.AddrOfPinnedObject(),
                sendData.Length,
                recvHandle.AddrOfPinnedObject(),
                (int)GlooDataType.Int32);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            sendHandle.Free();
            recvHandle.Free();
        }

        return recvData;
    }

    /// <summary>
    /// Gathers <c>long[]</c> data from all processes and returns the concatenated result.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="sendData">This process's contribution.</param>
    /// <returns>Concatenated data from all processes (length = sendData.Length * WorldSize).</returns>
    public static long[] AllGather(GlooContext context, long[] sendData)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (sendData == null) throw new ArgumentNullException(nameof(sendData));

        var recvData = new long[sendData.Length * context.WorldSize];
        var sendHandle = GCHandle.Alloc(sendData, GCHandleType.Pinned);
        var recvHandle = GCHandle.Alloc(recvData, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_allgather(
                context.Handle,
                sendHandle.AddrOfPinnedObject(),
                sendData.Length,
                recvHandle.AddrOfPinnedObject(),
                (int)GlooDataType.Int64);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            sendHandle.Free();
            recvHandle.Free();
        }

        return recvData;
    }

    // ─── Barrier ─────────────────────────────────────────────────────

    /// <summary>
    /// Blocks until all processes have reached the barrier.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    public static void Barrier(GlooContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        int result = GlooNativeLibrary.gloo_barrier(context.Handle);
        GlooException.ThrowIfFailed(result);
    }

    // ─── ReduceScatter ───────────────────────────────────────────────

    /// <summary>
    /// Performs a ReduceScatter on a <c>float[]</c> buffer. Each process receives a reduced chunk.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="sendData">The full data to reduce (length must be divisible by WorldSize).</param>
    /// <param name="op">The reduction operation to apply.</param>
    /// <returns>This process's reduced chunk (length = sendData.Length / WorldSize).</returns>
    public static float[] ReduceScatter(GlooContext context, float[] sendData, GlooReduceOp op)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (sendData == null) throw new ArgumentNullException(nameof(sendData));

        int chunkSize = sendData.Length / context.WorldSize;
        var recvData = new float[chunkSize];

        var sendHandle = GCHandle.Alloc(sendData, GCHandleType.Pinned);
        var recvHandle = GCHandle.Alloc(recvData, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_reduce_scatter(
                context.Handle,
                sendHandle.AddrOfPinnedObject(),
                recvHandle.AddrOfPinnedObject(),
                sendData.Length,
                (int)GlooDataType.Float32,
                (int)op);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            sendHandle.Free();
            recvHandle.Free();
        }

        return recvData;
    }

    /// <summary>
    /// Performs a ReduceScatter on a <c>double[]</c> buffer. Each process receives a reduced chunk.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="sendData">The full data to reduce (length must be divisible by WorldSize).</param>
    /// <param name="op">The reduction operation to apply.</param>
    /// <returns>This process's reduced chunk (length = sendData.Length / WorldSize).</returns>
    public static double[] ReduceScatter(GlooContext context, double[] sendData, GlooReduceOp op)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (sendData == null) throw new ArgumentNullException(nameof(sendData));

        int chunkSize = sendData.Length / context.WorldSize;
        var recvData = new double[chunkSize];

        var sendHandle = GCHandle.Alloc(sendData, GCHandleType.Pinned);
        var recvHandle = GCHandle.Alloc(recvData, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_reduce_scatter(
                context.Handle,
                sendHandle.AddrOfPinnedObject(),
                recvHandle.AddrOfPinnedObject(),
                sendData.Length,
                (int)GlooDataType.Float64,
                (int)op);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            sendHandle.Free();
            recvHandle.Free();
        }

        return recvData;
    }

    /// <summary>
    /// Performs a ReduceScatter on an <c>int[]</c> buffer. Each process receives a reduced chunk.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="sendData">The full data to reduce (length must be divisible by WorldSize).</param>
    /// <param name="op">The reduction operation to apply.</param>
    /// <returns>This process's reduced chunk (length = sendData.Length / WorldSize).</returns>
    public static int[] ReduceScatter(GlooContext context, int[] sendData, GlooReduceOp op)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (sendData == null) throw new ArgumentNullException(nameof(sendData));

        int chunkSize = sendData.Length / context.WorldSize;
        var recvData = new int[chunkSize];

        var sendHandle = GCHandle.Alloc(sendData, GCHandleType.Pinned);
        var recvHandle = GCHandle.Alloc(recvData, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_reduce_scatter(
                context.Handle,
                sendHandle.AddrOfPinnedObject(),
                recvHandle.AddrOfPinnedObject(),
                sendData.Length,
                (int)GlooDataType.Int32,
                (int)op);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            sendHandle.Free();
            recvHandle.Free();
        }

        return recvData;
    }

    /// <summary>
    /// Performs a ReduceScatter on a <c>long[]</c> buffer. Each process receives a reduced chunk.
    /// </summary>
    /// <param name="context">The Gloo context (must have transport attached).</param>
    /// <param name="sendData">The full data to reduce (length must be divisible by WorldSize).</param>
    /// <param name="op">The reduction operation to apply.</param>
    /// <returns>This process's reduced chunk (length = sendData.Length / WorldSize).</returns>
    public static long[] ReduceScatter(GlooContext context, long[] sendData, GlooReduceOp op)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (sendData == null) throw new ArgumentNullException(nameof(sendData));

        int chunkSize = sendData.Length / context.WorldSize;
        var recvData = new long[chunkSize];

        var sendHandle = GCHandle.Alloc(sendData, GCHandleType.Pinned);
        var recvHandle = GCHandle.Alloc(recvData, GCHandleType.Pinned);
        try
        {
            int result = GlooNativeLibrary.gloo_reduce_scatter(
                context.Handle,
                sendHandle.AddrOfPinnedObject(),
                recvHandle.AddrOfPinnedObject(),
                sendData.Length,
                (int)GlooDataType.Int64,
                (int)op);
            GlooException.ThrowIfFailed(result);
        }
        finally
        {
            sendHandle.Free();
            recvHandle.Free();
        }

        return recvData;
    }
}

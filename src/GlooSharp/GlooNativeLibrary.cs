using System;
using System.Runtime.InteropServices;

namespace GlooSharp;

/// <summary>
/// P/Invoke declarations for the native Gloo C shim library (<c>gloo_native</c>).
/// </summary>
/// <remarks>
/// <para>
/// All functions use <see cref="CallingConvention.Cdecl"/> and return an <c>int</c>
/// corresponding to a <see cref="GlooResult"/> error code.
/// </para>
/// <para>
/// The native library name is <c>gloo_native</c>, which resolves to:
/// <list type="bullet">
///   <item><description><c>gloo_native.dll</c> on Windows</description></item>
///   <item><description><c>libgloo_native.so</c> on Linux</description></item>
///   <item><description><c>libgloo_native.dylib</c> on macOS</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class GlooNativeLibrary
{
    private const string LibraryName = "gloo_native";

    // ─── Context lifecycle ───────────────────────────────────────────

    /// <summary>
    /// Creates a new Gloo context for the given rank and world size.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gloo_context_create(int rank, int size, out IntPtr ctx);

    /// <summary>
    /// Destroys a previously created Gloo context and frees native resources.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gloo_context_destroy(IntPtr ctx);

    // ─── Transport ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a TCP transport and attaches it to the context.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int gloo_transport_tcp_create(
        IntPtr ctx,
        [MarshalAs(UnmanagedType.LPStr)] string hostname,
        int port);

    /// <summary>
    /// Creates an InfiniBand transport and attaches it to the context.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int gloo_transport_ib_create(
        IntPtr ctx,
        [MarshalAs(UnmanagedType.LPStr)] string device);

    /// <summary>
    /// Checks whether InfiniBand devices are available on the system.
    /// Returns <see cref="GlooResult.Success"/> (0) if available, non-zero otherwise.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gloo_transport_ib_available();

    // ─── Collective operations ───────────────────────────────────────

    /// <summary>
    /// Performs an AllReduce collective operation.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gloo_allreduce(
        IntPtr ctx,
        IntPtr sendbuf,
        IntPtr recvbuf,
        int count,
        int datatype,
        int op);

    /// <summary>
    /// Performs a Broadcast collective operation from the specified root.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gloo_broadcast(
        IntPtr ctx,
        IntPtr buf,
        int count,
        int datatype,
        int root);

    /// <summary>
    /// Performs an AllGather collective operation.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gloo_allgather(
        IntPtr ctx,
        IntPtr sendbuf,
        int sendcount,
        IntPtr recvbuf,
        int datatype);

    /// <summary>
    /// Performs a Barrier synchronization across all processes.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gloo_barrier(IntPtr ctx);

    /// <summary>
    /// Performs a ReduceScatter collective operation.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gloo_reduce_scatter(
        IntPtr ctx,
        IntPtr sendbuf,
        IntPtr recvbuf,
        int count,
        int datatype,
        int op);
}

using System;

namespace GlooSharp;

/// <summary>
/// Factory methods for creating Gloo transport layers.
/// </summary>
/// <remarks>
/// <para><b>Overview:</b>
/// After creating a <see cref="GlooContext"/>, you must attach a transport before
/// performing any collective operations. The transport determines how data physically
/// moves between processes.
/// </para>
/// <para><b>For Beginners:</b>
/// A transport is the "road" that data travels on between processes:
/// <list type="bullet">
///   <item><description>
///     <b>TCP</b>: Uses standard networking (works everywhere).
///     Call <see cref="CreateTCP"/> with the hostname and port.
///   </description></item>
///   <item><description>
///     <b>InfiniBand</b>: Uses high-speed RDMA hardware (much faster, but requires
///     special network cards). Call <see cref="CreateInfiniBand"/> with the device name.
///   </description></item>
/// </list>
/// </para>
/// </remarks>
public static class GlooTransport
{
    /// <summary>
    /// Creates a TCP transport and attaches it to the given context.
    /// Uses a file-based rendezvous store so all processes can discover each other.
    /// </summary>
    /// <param name="context">The Gloo context to attach the transport to.</param>
    /// <param name="hostname">
    /// The hostname or IP address that this rank should bind to (e.g., "0.0.0.0" or "192.168.1.10").
    /// </param>
    /// <param name="port">The base port number. Each rank typically uses <c>port + rank</c>.</param>
    /// <param name="storePath">
    /// Path to a shared directory for rendezvous. All processes must be able to read/write
    /// this directory. If <c>null</c>, defaults to a temporary directory.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> or <paramref name="hostname"/> is null.</exception>
    /// <exception cref="GlooException">Thrown if the native transport creation fails.</exception>
    public static void CreateTCP(GlooContext context, string hostname, int port,
                                  string? storePath = null)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (hostname == null) throw new ArgumentNullException(nameof(hostname));

        string effectiveStorePath = storePath ?? GetDefaultStorePath();

        int result = GlooNativeLibrary.gloo_transport_tcp_create(
            context.Handle, hostname, port, effectiveStorePath);
        GlooException.ThrowIfFailed(result);
    }

    /// <summary>
    /// Creates an InfiniBand transport and attaches it to the given context.
    /// Uses a file-based rendezvous store so all processes can discover each other.
    /// </summary>
    /// <param name="context">The Gloo context to attach the transport to.</param>
    /// <param name="device">
    /// The InfiniBand device name (e.g., "mlx5_0"). Pass an empty string to auto-detect.
    /// </param>
    /// <param name="storePath">
    /// Path to a shared directory for rendezvous. All processes must be able to read/write
    /// this directory. If <c>null</c>, defaults to a temporary directory.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> or <paramref name="device"/> is null.</exception>
    /// <exception cref="GlooException">Thrown if the native transport creation fails.</exception>
    public static void CreateInfiniBand(GlooContext context, string device,
                                         string? storePath = null)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (device == null) throw new ArgumentNullException(nameof(device));

        string effectiveStorePath = storePath ?? GetDefaultStorePath();

        int result = GlooNativeLibrary.gloo_transport_ib_create(
            context.Handle, device, effectiveStorePath);
        GlooException.ThrowIfFailed(result);
    }

    /// <summary>
    /// Gets the default store path for rendezvous. Uses the GLOO_STORE_PATH environment
    /// variable if set, otherwise creates a temporary directory.
    /// </summary>
    private static string GetDefaultStorePath()
    {
        string? envPath = Environment.GetEnvironmentVariable("GLOO_STORE_PATH");
        if (!string.IsNullOrEmpty(envPath))
        {
            return envPath;
        }

        string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gloo_rendezvous");
        System.IO.Directory.CreateDirectory(tempPath);
        return tempPath;
    }

    /// <summary>
    /// Checks whether InfiniBand hardware is available on this system.
    /// </summary>
    /// <returns><c>true</c> if InfiniBand devices are detected; <c>false</c> otherwise.</returns>
    public static bool IsInfiniBandAvailable()
    {
        try
        {
            return GlooNativeLibrary.gloo_transport_ib_available() == (int)GlooResult.Success;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}

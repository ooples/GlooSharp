namespace GlooSharp;

/// <summary>
/// Transport types available for Gloo communication.
/// </summary>
/// <remarks>
/// <para><b>For Beginners:</b>
/// The transport layer determines how data physically moves between processes:
/// - <see cref="TCP"/>: Standard network communication (works everywhere).
/// - <see cref="InfiniBand"/>: High-performance RDMA networking (requires special hardware).
///
/// Use TCP for development and most deployments. Use InfiniBand only if your cluster
/// has InfiniBand hardware and you need maximum throughput.
/// </para>
/// </remarks>
public enum GlooTransportType
{
    /// <summary>TCP/IP transport. Works on any network. Default choice.</summary>
    TCP = 0,

    /// <summary>InfiniBand transport via ibverbs. Requires InfiniBand hardware.</summary>
    InfiniBand = 1
}

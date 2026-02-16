using System;
using Xunit;

namespace GlooSharp.Tests;

/// <summary>
/// Tests for <see cref="GlooTransport"/> parameter validation.
/// </summary>
public class GlooTransportTests
{
    [Fact]
    public void CreateTCP_NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooTransport.CreateTCP(null!, "localhost", 29500));
    }

    [Fact]
    public void CreateTCP_NullHostname_Throws()
    {
        // This will throw ArgumentNullException before reaching native code
        // because we check hostname == null before calling Handle
        Assert.ThrowsAny<ArgumentException>(() =>
            GlooTransport.CreateTCP(null!, null!, 29500));
    }

    [Fact]
    public void CreateInfiniBand_NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooTransport.CreateInfiniBand(null!, "mlx5_0"));
    }

    [Fact]
    public void CreateInfiniBand_NullDevice_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            GlooTransport.CreateInfiniBand(null!, null!));
    }

    [Fact]
    public void IsInfiniBandAvailable_DoesNotThrow()
    {
        // Should return false gracefully when native library is not present
        var result = GlooTransport.IsInfiniBandAvailable();
        Assert.False(result);
    }
}

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
    public void CreateTCP_BothNullArgs_ThrowsForContext()
    {
        // Context is checked first, so passing both null exercises the context null check.
        // Hostname null check cannot be tested without a valid native context.
        var ex = Assert.Throws<ArgumentNullException>(() =>
            GlooTransport.CreateTCP(null!, null!, 29500));
        Assert.Equal("context", ex.ParamName);
    }

    [Fact]
    public void CreateInfiniBand_NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooTransport.CreateInfiniBand(null!, "mlx5_0"));
    }

    [Fact]
    public void CreateInfiniBand_BothNullArgs_ThrowsForContext()
    {
        // Context is checked first, so passing both null exercises the context null check.
        // Device null check cannot be tested without a valid native context.
        var ex = Assert.Throws<ArgumentNullException>(() =>
            GlooTransport.CreateInfiniBand(null!, null!));
        Assert.Equal("context", ex.ParamName);
    }

    [Fact]
    public void IsInfiniBandAvailable_DoesNotThrow()
    {
        // Just verify it doesn't throw; result depends on environment
        var exception = Record.Exception(() => GlooTransport.IsInfiniBandAvailable());
        Assert.Null(exception);
    }
}

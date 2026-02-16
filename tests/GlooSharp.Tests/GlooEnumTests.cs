using Xunit;

namespace GlooSharp.Tests;

/// <summary>
/// Tests for GlooSharp enum types and their expected values.
/// These ensure the managed enums match the native C shim's integer codes.
/// </summary>
public class GlooEnumTests
{
    [Fact]
    public void GlooResult_HasCorrectValues()
    {
        Assert.Equal(0, (int)GlooResult.Success);
        Assert.Equal(1, (int)GlooResult.InvalidArgument);
        Assert.Equal(2, (int)GlooResult.SystemError);
        Assert.Equal(3, (int)GlooResult.TransportError);
        Assert.Equal(4, (int)GlooResult.Timeout);
        Assert.Equal(5, (int)GlooResult.NotInitialized);
        Assert.Equal(6, (int)GlooResult.InternalError);
    }

    [Fact]
    public void GlooDataType_HasCorrectValues()
    {
        Assert.Equal(0, (int)GlooDataType.Float32);
        Assert.Equal(1, (int)GlooDataType.Float64);
        Assert.Equal(2, (int)GlooDataType.Int32);
        Assert.Equal(3, (int)GlooDataType.Int64);
    }

    [Fact]
    public void GlooReduceOp_HasCorrectValues()
    {
        Assert.Equal(0, (int)GlooReduceOp.Sum);
        Assert.Equal(1, (int)GlooReduceOp.Product);
        Assert.Equal(2, (int)GlooReduceOp.Min);
        Assert.Equal(3, (int)GlooReduceOp.Max);
    }

    [Fact]
    public void GlooTransportType_HasCorrectValues()
    {
        Assert.Equal(0, (int)GlooTransportType.TCP);
        Assert.Equal(1, (int)GlooTransportType.InfiniBand);
    }
}

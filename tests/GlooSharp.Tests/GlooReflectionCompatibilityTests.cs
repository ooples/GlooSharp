using System;
using System.Reflection;
using Xunit;

namespace GlooSharp.Tests;

/// <summary>
/// Verifies that GlooSharp types and method signatures match what
/// AiDotNet.DistributedTraining.GlooCommunicationBackend expects to find via reflection.
/// If any of these tests fail, the reflection-based detection in GlooCommunicationBackend
/// will silently fall back to TCP, so these are critical compatibility tests.
/// </summary>
public class GlooReflectionCompatibilityTests
{
    [Fact]
    public void GlooContext_IsDiscoverableByFullName()
    {
        var type = Type.GetType("GlooSharp.GlooContext, GlooSharp");
        Assert.NotNull(type);
    }

    [Fact]
    public void GlooCollectives_IsDiscoverableByFullName()
    {
        var type = Type.GetType("GlooSharp.GlooCollectives, GlooSharp");
        Assert.NotNull(type);
    }

    [Fact]
    public void GlooTransport_IsDiscoverableByFullName()
    {
        var type = Type.GetType("GlooSharp.GlooTransport, GlooSharp");
        Assert.NotNull(type);
    }

    [Fact]
    public void GlooReduceOp_IsDiscoverableByFullName()
    {
        var type = Type.GetType("GlooSharp.GlooReduceOp, GlooSharp");
        Assert.NotNull(type);
    }

    [Fact]
    public void GlooContext_HasConstructor_RankWorldSize()
    {
        var type = typeof(GlooContext);
        var ctor = type.GetConstructor(new[] { typeof(int), typeof(int) });
        Assert.NotNull(ctor);
    }

    [Fact]
    public void GlooContext_ImplementsIDisposable()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(GlooContext)));
    }

    [Fact]
    public void GlooTransport_HasCreateTCP_WithStorePath()
    {
        var ctxType = typeof(GlooContext);
        var method = typeof(GlooTransport).GetMethod("CreateTCP",
            new[] { ctxType, typeof(string), typeof(int), typeof(string) });
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void GlooTransport_HasCreateInfiniBand_WithStorePath()
    {
        var ctxType = typeof(GlooContext);
        var method = typeof(GlooTransport).GetMethod("CreateInfiniBand",
            new[] { ctxType, typeof(string), typeof(string) });
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void GlooCollectives_HasAllReduce_DoubleOverload()
    {
        var ctxType = typeof(GlooContext);
        var opType = typeof(GlooReduceOp);
        var method = typeof(GlooCollectives).GetMethod("AllReduce",
            new[] { ctxType, typeof(double[]), opType });
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void GlooCollectives_HasAllReduce_FloatOverload()
    {
        var ctxType = typeof(GlooContext);
        var opType = typeof(GlooReduceOp);
        var method = typeof(GlooCollectives).GetMethod("AllReduce",
            new[] { ctxType, typeof(float[]), opType });
        Assert.NotNull(method);
    }

    [Fact]
    public void GlooCollectives_HasAllReduce_IntOverload()
    {
        var ctxType = typeof(GlooContext);
        var opType = typeof(GlooReduceOp);
        var method = typeof(GlooCollectives).GetMethod("AllReduce",
            new[] { ctxType, typeof(int[]), opType });
        Assert.NotNull(method);
    }

    [Fact]
    public void GlooCollectives_HasAllReduce_LongOverload()
    {
        var ctxType = typeof(GlooContext);
        var opType = typeof(GlooReduceOp);
        var method = typeof(GlooCollectives).GetMethod("AllReduce",
            new[] { ctxType, typeof(long[]), opType });
        Assert.NotNull(method);
    }

    [Fact]
    public void GlooCollectives_HasBroadcast_DoubleOverload()
    {
        var ctxType = typeof(GlooContext);
        var method = typeof(GlooCollectives).GetMethod("Broadcast",
            new[] { ctxType, typeof(double[]), typeof(int) });
        Assert.NotNull(method);
    }

    [Fact]
    public void GlooCollectives_HasAllGather_DoubleOverload()
    {
        var ctxType = typeof(GlooContext);
        var method = typeof(GlooCollectives).GetMethod("AllGather",
            new[] { ctxType, typeof(double[]) });
        Assert.NotNull(method);
    }

    [Fact]
    public void GlooCollectives_HasBarrier()
    {
        var ctxType = typeof(GlooContext);
        var method = typeof(GlooCollectives).GetMethod("Barrier",
            new[] { ctxType });
        Assert.NotNull(method);
    }

    [Fact]
    public void GlooCollectives_HasReduceScatter_DoubleOverload()
    {
        var ctxType = typeof(GlooContext);
        var opType = typeof(GlooReduceOp);
        var method = typeof(GlooCollectives).GetMethod("ReduceScatter",
            new[] { ctxType, typeof(double[]), opType });
        Assert.NotNull(method);
    }

    [Fact]
    public void GlooReduceOp_HasExpectedValues()
    {
        // These must match the native GLOO_OP_* constants
        Assert.Equal(0, (int)GlooReduceOp.Sum);
        Assert.Equal(1, (int)GlooReduceOp.Product);
        Assert.Equal(2, (int)GlooReduceOp.Min);
        Assert.Equal(3, (int)GlooReduceOp.Max);
    }

    [Fact]
    public void GlooReduceOp_CanBeCreatedViaEnumToObject()
    {
        // GlooCommunicationBackend uses Enum.ToObject to map reduction operations
        var opType = typeof(GlooReduceOp);
        var sumOp = Enum.ToObject(opType, 0);
        Assert.Equal(GlooReduceOp.Sum, sumOp);

        var maxOp = Enum.ToObject(opType, 3);
        Assert.Equal(GlooReduceOp.Max, maxOp);
    }

    [Fact]
    public void GlooDataType_MatchesNativeConstants()
    {
        Assert.Equal(0, (int)GlooDataType.Float32);
        Assert.Equal(1, (int)GlooDataType.Float64);
        Assert.Equal(2, (int)GlooDataType.Int32);
        Assert.Equal(3, (int)GlooDataType.Int64);
    }

    [Fact]
    public void GlooResult_SuccessIsZero()
    {
        Assert.Equal(0, (int)GlooResult.Success);
    }

    [Fact]
    public void GlooTransportType_HasExpectedValues()
    {
        Assert.Equal(0, (int)GlooTransportType.TCP);
        Assert.Equal(1, (int)GlooTransportType.InfiniBand);
    }
}

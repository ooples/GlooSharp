using System;
using Xunit;

namespace GlooSharp.Tests;

/// <summary>
/// Tests for <see cref="GlooCollectives"/> parameter validation.
/// These test managed-side argument checks only (no native library required).
/// </summary>
public class GlooCollectivesTests
{
    [Fact]
    public void AllReduce_NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooCollectives.AllReduce(null!, new float[] { 1.0f }, GlooReduceOp.Sum));
    }

    [Fact]
    public void AllReduce_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooCollectives.AllReduce(null!, (float[])null!, GlooReduceOp.Sum));
    }

    [Fact]
    public void Broadcast_NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooCollectives.Broadcast(null!, new float[] { 1.0f }, 0));
    }

    [Fact]
    public void Broadcast_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooCollectives.Broadcast(null!, (float[])null!, 0));
    }

    [Fact]
    public void AllGather_NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooCollectives.AllGather(null!, new float[] { 1.0f }));
    }

    [Fact]
    public void AllGather_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooCollectives.AllGather(null!, (float[])null!));
    }

    [Fact]
    public void Barrier_NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooCollectives.Barrier(null!));
    }

    [Fact]
    public void ReduceScatter_NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooCollectives.ReduceScatter(null!, new float[] { 1.0f }, GlooReduceOp.Sum));
    }

    [Fact]
    public void ReduceScatter_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GlooCollectives.ReduceScatter(null!, (float[])null!, GlooReduceOp.Sum));
    }
}

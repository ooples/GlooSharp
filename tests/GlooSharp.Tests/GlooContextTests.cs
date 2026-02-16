using System;
using Xunit;

namespace GlooSharp.Tests;

/// <summary>
/// Tests for <see cref="GlooContext"/> parameter validation.
/// Native library is not required for these tests — they verify managed-side validation.
/// </summary>
public class GlooContextTests
{
    [Fact]
    public void Constructor_NegativeRank_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GlooContext(-1, 4));
    }

    [Fact]
    public void Constructor_RankEqualToWorldSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GlooContext(4, 4));
    }

    [Fact]
    public void Constructor_RankGreaterThanWorldSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GlooContext(5, 4));
    }

    [Fact]
    public void Constructor_ZeroWorldSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GlooContext(0, 0));
    }

    [Fact]
    public void Constructor_NegativeWorldSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GlooContext(0, -1));
    }
}

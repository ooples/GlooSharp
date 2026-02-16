using System;
using Xunit;

namespace GlooSharp.Tests;

/// <summary>
/// Tests for <see cref="GlooException"/> error handling.
/// </summary>
public class GlooExceptionTests
{
    [Fact]
    public void ThrowIfFailed_DoesNotThrow_OnSuccess()
    {
        var exception = Record.Exception(() => GlooException.ThrowIfFailed(0));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(1, GlooResult.InvalidArgument)]
    [InlineData(2, GlooResult.SystemError)]
    [InlineData(3, GlooResult.TransportError)]
    [InlineData(4, GlooResult.Timeout)]
    [InlineData(5, GlooResult.NotInitialized)]
    [InlineData(6, GlooResult.InternalError)]
    public void ThrowIfFailed_Throws_OnErrorCode(int errorCode, GlooResult expectedResult)
    {
        var ex = Assert.Throws<GlooException>(() => GlooException.ThrowIfFailed(errorCode));
        Assert.Equal(expectedResult, ex.Result);
        Assert.NotEmpty(ex.Message);
    }

    [Fact]
    public void Constructor_WithResult_SetsPropertiesCorrectly()
    {
        var ex = new GlooException(GlooResult.Timeout);
        Assert.Equal(GlooResult.Timeout, ex.Result);
        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithCustomMessage_PreservesMessage()
    {
        var ex = new GlooException(GlooResult.SystemError, "Custom error message");
        Assert.Equal(GlooResult.SystemError, ex.Result);
        Assert.Equal("Custom error message", ex.Message);
    }

    [Fact]
    public void ThrowIfFailed_UnknownCode_ProducesDescriptiveMessage()
    {
        var ex = Assert.Throws<GlooException>(() => GlooException.ThrowIfFailed(99));
        Assert.Contains("99", ex.Message);
    }
}

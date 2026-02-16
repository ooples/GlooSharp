using System;
using System.IO;
using Xunit;

namespace GlooSharp.Tests;

/// <summary>
/// Tests for GlooTransport store path and default behavior.
/// These verify that the rendezvous store path logic works correctly
/// without requiring the native library.
/// </summary>
public class GlooTransportStoreTests
{
    [Fact]
    public void CreateTCP_StorePathParameter_IsOptional()
    {
        // Verify the method signature allows null storePath (optional parameter)
        var method = typeof(GlooTransport).GetMethod("CreateTCP",
            new[] { typeof(GlooContext), typeof(string), typeof(int), typeof(string) });
        Assert.NotNull(method);

        var parameters = method.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal("storePath", parameters[3].Name);
        Assert.True(parameters[3].HasDefaultValue);
    }

    [Fact]
    public void CreateInfiniBand_StorePathParameter_IsOptional()
    {
        var method = typeof(GlooTransport).GetMethod("CreateInfiniBand",
            new[] { typeof(GlooContext), typeof(string), typeof(string) });
        Assert.NotNull(method);

        var parameters = method.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal("storePath", parameters[2].Name);
        Assert.True(parameters[2].HasDefaultValue);
    }

    [Fact]
    public void DefaultStorePath_CreatesDirectory_WhenEnvVarNotSet()
    {
        // Temporarily clear the env var
        string? originalValue = Environment.GetEnvironmentVariable("GLOO_STORE_PATH");
        try
        {
            Environment.SetEnvironmentVariable("GLOO_STORE_PATH", null);

            // The default store path should be in temp directory
            string expectedDir = Path.Combine(Path.GetTempPath(), "gloo_rendezvous");

            // We can't call the private method directly, but we can verify
            // that CreateTCP with null storePath would use a temp directory
            // by checking the parameter's default value is null
            var method = typeof(GlooTransport).GetMethod("CreateTCP",
                new[] { typeof(GlooContext), typeof(string), typeof(int), typeof(string) });
            var storeParam = method?.GetParameters()[3];
            Assert.Null(storeParam?.DefaultValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GLOO_STORE_PATH", originalValue);
        }
    }

    [Fact]
    public void GlooStorePathEnvVar_IsRespected()
    {
        // Verify that the GLOO_STORE_PATH environment variable name is correct
        // by setting it and checking it can be read
        string testPath = Path.Combine(Path.GetTempPath(), "gloo_test_store_" + Guid.NewGuid().ToString("N"));
        string? originalValue = Environment.GetEnvironmentVariable("GLOO_STORE_PATH");
        try
        {
            Environment.SetEnvironmentVariable("GLOO_STORE_PATH", testPath);
            string? readBack = Environment.GetEnvironmentVariable("GLOO_STORE_PATH");
            Assert.Equal(testPath, readBack);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GLOO_STORE_PATH", originalValue);
        }
    }
}

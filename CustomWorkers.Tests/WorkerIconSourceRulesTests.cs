using System;
using Xunit;

namespace CustomWorkers.Tests;

public sealed class WorkerIconSourceRulesTests
{
    [Fact]
    public void TryDecodeBase64Png_ReturnsBytesForValidBase64()
    {
        string base64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        Assert.True(Base64SpriteHelper.TryDecodePng(base64, out byte[]? pngBytes));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, pngBytes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base64")]
    public void TryDecodeBase64Png_RejectsInvalidInput(string? encoded)
    {
        Assert.False(Base64SpriteHelper.TryDecodePng(encoded, out _));
    }
}

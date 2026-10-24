using System;
using System.IO;
using Xunit;

namespace CustomWorkers.Tests;

public sealed class ArcAssetIsolationRulesTests
{
    [Fact]
    public void EnsureClone_GuardsOwnedAssetApplyBehindArcAssetLoaderSwitch()
    {
        string source = ReadWorkspaceFile("helpers/hirescreenclonehelper.cs");
        string methodBlock = ExtractMethodBlock(source, "internal static HireWorkerScreen? EnsureClone(PhoneManager? phoneManager, out bool createdThisCall)");

        Assert.Contains("if (KillSwitchHelper.IsArcAssetLoaderEnabled())", methodBlock, StringComparison.Ordinal);
        Assert.Contains("ArcRecruiterAssetLayerHelper.ApplyOwnedAssets", methodBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyTileVisuals_GuardsTileIconApplyBehindArcAssetLoaderSwitch()
    {
        string source = ReadWorkspaceFile("helpers/phoneoverhaulapphelper.cs");
        string methodBlock = ExtractMethodBlock(source, "internal static void ApplyTileVisuals(UI_PhoneScreen phoneScreen)");

        Assert.Contains("if (KillSwitchHelper.IsArcAssetLoaderEnabled())", methodBlock, StringComparison.Ordinal);
        Assert.Contains("ArcRecruiterAssetLayerHelper.ApplyTileIcon", methodBlock, StringComparison.Ordinal);
    }

    private static string ReadWorkspaceFile(string relativePath)
    {
        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            string candidate = Path.Combine(current, "CustomWorkers.Tests", "CustomWorkers.Tests.csproj");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(Path.Combine(current, relativePath));
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Could not locate workspace root for CustomWorkers tests.");
    }

    private static string ExtractMethodBlock(string source, string methodSignature)
    {
        int signatureIndex = source.IndexOf(methodSignature, StringComparison.Ordinal);
        Assert.True(signatureIndex >= 0, $"Could not find method signature: {methodSignature}");

        int blockStart = source.IndexOf('{', signatureIndex);
        Assert.True(blockStart >= 0, $"Could not find block start for: {methodSignature}");

        int depth = 0;
        for (int index = blockStart; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(signatureIndex, index - signatureIndex + 1);
                }
            }
        }

        throw new InvalidOperationException($"Could not find method end for: {methodSignature}");
    }
}

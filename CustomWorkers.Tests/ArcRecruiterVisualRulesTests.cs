using System;
using System.IO;
using Xunit;

namespace CustomWorkers.Tests;

public sealed class ArcRecruiterVisualRulesTests
{
    [Fact]
    public void ShouldAssignTileIcon_TargetsPrimaryTileIconAndControllerSelectorSprites()
    {
        Assert.True(ArcRecruiterVisualRules.ShouldAssignTileIcon("Icon", "PhoneApp_0005_CustomWorkers_ArcRecruiter"));
        Assert.True(ArcRecruiterVisualRules.ShouldAssignTileIcon("Sprite", "ControllerSelectorUIPos"));
        Assert.True(ArcRecruiterVisualRules.ShouldAssignTileIcon("Sprite", "ControllerSelectorUIPos (2)"));
    }

    [Fact]
    public void ShouldAssignTileIcon_RejectsSecondaryPhoneTileIcon()
    {
        Assert.False(ArcRecruiterVisualRules.ShouldAssignTileIcon("Icon2", "PhoneApp_0005_CustomWorkers_ArcRecruiter"));
        Assert.True(ArcRecruiterVisualRules.ShouldHideSecondaryTileIcon("Icon2"));
    }

    [Fact]
    public void ShouldHideLegacyHeaderBranding_TargetsRootGoHireWidgetsOnly()
    {
        Assert.True(ArcRecruiterVisualRules.ShouldHideLegacyHeaderText("TitleText", isUnderHireWorkerPanel: false));
        Assert.True(ArcRecruiterVisualRules.ShouldHideLegacyHeaderIcon("Icon", isDirectChildOfScreenGroup: true, isUnderHireWorkerPanel: false));

        Assert.False(ArcRecruiterVisualRules.ShouldHideLegacyHeaderText("TitleText", isUnderHireWorkerPanel: true));
        Assert.False(ArcRecruiterVisualRules.ShouldHideLegacyHeaderIcon("Icon", isDirectChildOfScreenGroup: false, isUnderHireWorkerPanel: false));
    }

    [Fact]
    public void SourceFiles_HookVisualRulesAndScrollClamp()
    {
        string assetLayerSource = ReadWorkspaceFile("helpers/arcrecruiterassetlayerhelper.cs");
        string tileMethod = ExtractMethodBlock(assetLayerSource, "internal static bool ApplyTileIcon(Transform tile, ManualLogSource? logger)");
        string headerMethod = ExtractMethodBlock(assetLayerSource, "private static void ApplyHeaderLogo(HireWorkerScreen screen, ManualLogSource? logger)");
        string scrollSource = ReadWorkspaceFile("helpers/scrollhelper.cs");

        Assert.Contains("ArcRecruiterVisualRules.ShouldAssignTileIcon", tileMethod, StringComparison.Ordinal);
        Assert.Contains("ArcRecruiterVisualRules.ShouldHideSecondaryTileIcon", tileMethod, StringComparison.Ordinal);
        Assert.Contains("HideLegacyHeaderBranding", headerMethod, StringComparison.Ordinal);
        Assert.Contains("ClampExpandedHireWorkerScroll", scrollSource, StringComparison.Ordinal);
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

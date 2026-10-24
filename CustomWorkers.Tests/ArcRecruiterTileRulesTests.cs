using Xunit;
using System;
using System.IO;

namespace CustomWorkers.Tests;

public sealed class ArcRecruiterTileRulesTests
{
    [Theory]
    [InlineData("PhoneApp_0005_CustomWorkers_ArcRecruiter")]
    [InlineData("PhoneApp_0123_CustomWorkers_ArcRecruiter")]
    public void IsArcRecruiterTileName_MatchesActualPhoneTiles(string transformName)
    {
        Assert.True(ArcRecruiterTileRules.IsArcRecruiterTileName(transformName));
    }

    [Theory]
    [InlineData("CustomWorkers_ArcRecruiterHeaderLogo")]
    [InlineData("CustomWorkers_ArcRecruiter")]
    [InlineData("PhoneFolder_CustomWorkers_ArcRecruiter")]
    [InlineData("PhoneApp_0005_CustomWorkers_ArcRecruiter_Child")]
    public void IsArcRecruiterTileName_RejectsNonTileArcObjects(string transformName)
    {
        Assert.False(ArcRecruiterTileRules.IsArcRecruiterTileName(transformName));
    }

    [Fact]
    public void FindArcRecruiterTiles_UsesTileRuleInsteadOfSubstringMatch()
    {
        string source = ReadWorkspaceFile("helpers/phoneoverhaulapphelper.cs");
        string methodBlock = ExtractMethodBlock(source, "private static List<Transform> FindArcRecruiterTiles(Transform root)");

        Assert.Contains("ArcRecruiterTileRules.IsArcRecruiterTileName", methodBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("IndexOf(expectedSuffix", methodBlock, StringComparison.Ordinal);
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

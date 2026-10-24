using System.IO;
using Xunit;

namespace CustomWorkers.Tests;

public sealed class LogHelperSourceTests
{
    [Fact]
    public void BroadUiAssetHtmlDumpWriters_RemainDisabled()
    {
        string source = File.ReadAllText(Path.Combine(LocateSourceRoot(), "helpers", "loghelper.cs"));

        Assert.DoesNotContain("wrote app layout snapshot", source);
        Assert.DoesNotContain("wrote UI asset inventory", source);
        Assert.Contains("broad UI asset dumps are disabled", source);
    }

    private static string LocateSourceRoot()
    {
        string current = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "CustomWorkers.csproj"))
                && File.Exists(Path.Combine(current, "helpers", "loghelper.cs")))
            {
                return current;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the Custom Workers source root.");
    }
}

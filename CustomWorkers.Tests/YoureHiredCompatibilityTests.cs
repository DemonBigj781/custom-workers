using System;
using System.IO;
using Xunit;

namespace CustomWorkers.Tests;

public sealed class YoureHiredCompatibilityTests
{
    [Fact]
    public void WorkerManagerInitPatch_RunsAfterYoureHired()
    {
        string source = ReadPluginSource();

        Assert.Contains(
            "[HarmonyAfter(\"YoureHired\")]\n    private static class WorkerManagerInitPatch",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void HireWorkerScreenInitPatch_RunsAfterYoureHired()
    {
        string source = ReadPluginSource();

        Assert.Contains(
            "[HarmonyAfter(\"YoureHired\")]\n    private static class HireWorkerScreenInitPatch",
            source,
            StringComparison.Ordinal);
    }

    private static string ReadPluginSource()
    {
        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            string projectFile = Path.Combine(current, "CustomWorkers.Tests", "CustomWorkers.Tests.csproj");
            if (File.Exists(projectFile))
            {
                return File.ReadAllText(Path.Combine(current, "Plugin.cs"));
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Could not locate the Custom Workers source root.");
    }
}

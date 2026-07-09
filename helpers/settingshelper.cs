using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using BepInEx.Logging;

namespace CustomWorkers;

internal static class SettingsHelper
{
    internal static string GetFullLogPath()
    {
        return Path.Combine(Paths.BepInExRootPath, "LogOutput.log");
    }

    internal static string GetSplitLogPath()
    {
        return Path.Combine(Paths.BepInExRootPath, "LogOutputSplit", $"{Plugin.PluginGuid}.log");
    }

    internal static string GetConfigStamp()
    {
        string configPath = AppearanceSettingsHelper.GetConfigFilePath();
        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
        {
            return $"Custom Workers config stamp: attempt={BuildInfo.BuildAttempt} compileUtc={BuildInfo.BuildTimestampUtc} compileLocal={BuildInfo.BuildTimestampLocal} runId={TimestampHelper.GetRunId()} path=<missing>";
        }

        FileInfo fileInfo = new FileInfo(configPath);
        return $"Custom Workers config stamp: attempt={BuildInfo.BuildAttempt} compileUtc={BuildInfo.BuildTimestampUtc} compileLocal={BuildInfo.BuildTimestampLocal} runId={TimestampHelper.GetRunId()} path={configPath} fileTimestamp={fileInfo.LastWriteTime:O} size={fileInfo.Length} sha256={GetFileShaPrefix(configPath)}";
    }

    internal static string GetRunHeaderLine()
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        string fullLogPath = GetFullLogPath();
        string splitLogPath = GetSplitLogPath();
        return $"Custom Workers run header: runId={TimestampHelper.GetRunId()} buildAttempt={BuildInfo.BuildAttempt} compileUtc={BuildInfo.BuildTimestampUtc} compileLocal={BuildInfo.BuildTimestampLocal} configuration={BuildInfo.BuildConfiguration} dllSha256={GetFileShaPrefix(assemblyPath)} configSha256={GetFileShaPrefix(AppearanceSettingsHelper.GetConfigFilePath())} runtimeMode={KillSwitchHelper.GetRuntimeMode()} fullLogPath={fullLogPath} splitLogPath={splitLogPath} splitLogsArePartial=True eplLoaded={KillSwitchSettingsHelper.IsEnhancedPrefabLoaderLikelyLoaded()}";
    }

    internal static void WriteDirectRunHeader(ManualLogSource logger)
    {
        string runDirectory = LogHelper.GetRunArtifactDirectory();
        Directory.CreateDirectory(runDirectory);
        string outputPath = Path.Combine(runDirectory, "run-header.txt");

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(GetRunHeaderLine());
        builder.AppendLine(GetConfigStamp());
        builder.AppendLine(KillSwitchSettingsHelper.GetKillSwitchSummary());
        builder.AppendLine($"Custom Workers evidence policy: fullLogPath={GetFullLogPath()} is canonical for startup order. splitLogPath={GetSplitLogPath()} is a partial live filtered view until proven otherwise by splitter headers.");

        File.WriteAllText(outputPath, builder.ToString());
        logger.LogInfo($"Custom Workers wrote direct run header to {outputPath}.");
    }

    private static string GetFileShaPrefix(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return "<missing>";
        }

        using (FileStream stream = File.OpenRead(path))
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0, 12);
        }
    }
}

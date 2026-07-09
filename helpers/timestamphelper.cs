using System;
using System.Globalization;
using UnityEngine;

namespace CustomWorkers;

internal static class TimestampHelper
{
    private static readonly string RunId = Guid.NewGuid().ToString("N");

    internal static string GetRunId()
    {
        return RunId;
    }

    internal static string GetLogPrefix()
    {
        string wallClock = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        string realtime = Time.realtimeSinceStartup.ToString("0.000", CultureInfo.InvariantCulture);
        return $"[{wallClock} | rt={realtime} | run={RunId}] ";
    }

    internal static string Stamp(string message)
    {
        return GetLogPrefix() + message;
    }
}

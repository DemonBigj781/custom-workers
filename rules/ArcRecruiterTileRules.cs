using System;

namespace CustomWorkers;

internal static class ArcRecruiterTileRules
{
    private const string AppId = "CustomWorkers_ArcRecruiter";
    private const string PhoneAppPrefix = "PhoneApp_";
    private static readonly string ExpectedSuffix = "_" + AppId;

    internal static bool IsArcRecruiterTileName(string? transformName)
    {
        if (string.IsNullOrEmpty(transformName))
        {
            return false;
        }

        string nonNullTransformName = transformName!;
        return nonNullTransformName.StartsWith(PhoneAppPrefix, StringComparison.Ordinal)
            && nonNullTransformName.EndsWith(ExpectedSuffix, StringComparison.Ordinal);
    }
}

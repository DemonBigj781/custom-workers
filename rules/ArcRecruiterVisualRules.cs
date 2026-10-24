using System;

namespace CustomWorkers;

internal static class ArcRecruiterVisualRules
{
    internal static bool ShouldAssignTileIcon(string objectName, string parentName)
    {
        return string.Equals(objectName, "Icon", StringComparison.Ordinal)
            || (string.Equals(objectName, "Sprite", StringComparison.Ordinal)
                && parentName.StartsWith("ControllerSelectorUIPos", StringComparison.Ordinal));
    }

    internal static bool ShouldHideSecondaryTileIcon(string objectName)
    {
        return string.Equals(objectName, "Icon2", StringComparison.Ordinal);
    }

    internal static bool ShouldHideLegacyHeaderText(string objectName, bool isUnderHireWorkerPanel)
    {
        return !isUnderHireWorkerPanel && string.Equals(objectName, "TitleText", StringComparison.Ordinal);
    }

    internal static bool ShouldHideLegacyHeaderIcon(string objectName, bool isDirectChildOfScreenGroup, bool isUnderHireWorkerPanel)
    {
        return !isUnderHireWorkerPanel
            && isDirectChildOfScreenGroup
            && string.Equals(objectName, "Icon", StringComparison.Ordinal);
    }
}

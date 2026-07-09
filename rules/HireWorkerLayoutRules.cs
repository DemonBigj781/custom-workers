namespace CustomWorkers;

internal static class HireWorkerLayoutRules
{
    internal const int VanillaHireWorkerPanelCount = 8;

    internal static bool NeedsLayoutRefresh(int panelCountBefore, int panelCountAfter)
    {
        return panelCountAfter > panelCountBefore;
    }

    internal static bool ShouldRefreshExpandedLayout(int panelCount)
    {
        return panelCount > VanillaHireWorkerPanelCount;
    }
}

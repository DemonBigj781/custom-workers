namespace CustomWorkers;

internal static class HireWorkerLayoutRules
{
    internal static bool NeedsLayoutRefresh(int panelCountBefore, int panelCountAfter)
    {
        return panelCountAfter > panelCountBefore;
    }
}

namespace CustomWorkers;

internal static class ScrollStabilizationRules
{
    internal static int GetRefreshPassCount(int panelCountBefore, int panelCountAfter)
    {
        return panelCountAfter > panelCountBefore ? 3 : 1;
    }
}

namespace CustomWorkers;

internal static class HireWorkerCloneRules
{
    internal static int GetVisibleWorkerCount(bool isCloneScreen, int totalWorkerCount)
    {
        if (isCloneScreen)
        {
            return totalWorkerCount;
        }

        return totalWorkerCount < HireWorkerLayoutRules.VanillaHireWorkerPanelCount
            ? totalWorkerCount
            : HireWorkerLayoutRules.VanillaHireWorkerPanelCount;
    }
}

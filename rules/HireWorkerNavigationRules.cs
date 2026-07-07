namespace CustomWorkers;

internal static class HireWorkerNavigationRules
{
    internal static (int? up, int? down) GetPanelNeighbors(int panelCount, int panelIndex)
    {
        int? up = panelIndex > 0 ? panelIndex - 1 : null;
        int? down = panelIndex + 1 < panelCount ? panelIndex + 1 : null;
        return (up, down);
    }
}

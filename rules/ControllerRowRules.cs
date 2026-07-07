namespace CustomWorkers;

internal static class ControllerRowRules
{
    internal static bool NeedsAdditionalRows(int existingRowCount, int panelCount)
    {
        return existingRowCount < panelCount;
    }

    internal static int GetMissingRowCount(int existingRowCount, int panelCount)
    {
        return panelCount > existingRowCount ? panelCount - existingRowCount : 0;
    }
}

namespace CustomWorkers;

internal static class MrBurnsRules
{
    internal static float OverrideMoneyCost(bool enabled, float original)
    {
        return enabled ? 0f : original;
    }

    internal static int OverrideRequiredLevel(bool enabled, int original)
    {
        return enabled ? 0 : original;
    }
}

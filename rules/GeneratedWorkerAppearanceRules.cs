namespace CustomWorkers;

internal static class GeneratedWorkerAppearanceRules
{
    internal static string GetCharacterName(bool isFemale, int characterModelIndex)
    {
        return isFemale ? $"Female{characterModelIndex}" : $"Male{characterModelIndex}";
    }

    internal static bool ShouldApplyGeneratedCharacter(bool hasCharacterCustomization)
    {
        return hasCharacterCustomization;
    }
}

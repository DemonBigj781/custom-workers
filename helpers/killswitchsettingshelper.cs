using System;
using System.Linq;

namespace CustomWorkers;

internal static class KillSwitchSettingsHelper
{
    internal static string GetKillSwitchSummary()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        return $"Custom Workers kill-switch summary: attempt={BuildInfo.BuildAttempt} compileUtc={BuildInfo.BuildTimestampUtc} compileLocal={BuildInfo.BuildTimestampLocal} runtimeMode={KillSwitchHelper.GetRuntimeMode()} enableMod={options.EnableMod} arcUi={options.EnableArcUi} arcAssetLoader={options.EnableArcAssetLoader} phoneOverhaulHooks={options.EnablePhoneOverhaulHooks} rosterExtension={options.EnableRosterExtension} workerRuntime={options.EnableWorkerRuntimePatch} npcMutator={options.EnableNpcMutator} customerPatch={options.EnableCustomerAppearancePatch} workerPatch={options.EnableWorkerAppearancePatch} workerModelValidation={options.EnableGeneratedWorkerModelValidation} debugPopulation={options.DebugMapPopulationEvery10Seconds}";
    }

    internal static bool IsEnhancedPrefabLoaderLikelyLoaded()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(assembly => assembly.GetName().Name?.IndexOf("Prefab", StringComparison.OrdinalIgnoreCase) >= 0
                || assembly.GetName().Name?.IndexOf("EPL", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}

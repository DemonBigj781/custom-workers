using System.Collections.Generic;
using BepInEx.Logging;

namespace CustomWorkers;

internal static class KillSwitchHelper
{
    internal enum RuntimeMode
    {
        Disabled,
        DiagnosticsOnly,
        UiOnly,
        RosterOnly,
        AppearanceOnly,
        Full,
    }

    internal static RuntimeMode GetRuntimeMode()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        if (options.RuntimeMode != RuntimeMode.Full)
        {
            return options.RuntimeMode;
        }

        if (!options.EnableMod)
        {
            return RuntimeMode.Disabled;
        }

        bool arcUi = options.EnableArcUi || options.EnableArcAssetLoader || options.EnablePhoneOverhaulHooks;
        bool roster = options.EnableRosterExtension;
        bool appearance = options.EnableWorkerRuntimePatch
            || options.EnableNpcMutator
            || options.EnableCustomerAppearancePatch
            || options.EnableWorkerAppearancePatch
            || options.EnableGeneratedWorkerModelValidation;

        if (!arcUi && !roster && !appearance)
        {
            return RuntimeMode.DiagnosticsOnly;
        }

        if (arcUi && !roster && !appearance)
        {
            return RuntimeMode.UiOnly;
        }

        if (!arcUi && roster && !appearance)
        {
            return RuntimeMode.RosterOnly;
        }

        if (!arcUi && !roster && appearance)
        {
            return RuntimeMode.AppearanceOnly;
        }

        return RuntimeMode.Full;
    }

    internal static bool IsWorldReadyForRosterMutation()
    {
        if (!IsRosterExtensionEnabled())
        {
            return false;
        }

        List<Worker> workers = WorkerManager.GetWorkerList();
        if (workers == null || workers.Count <= 0)
        {
            return false;
        }

        WorkerManager? manager = CSingleton<WorkerManager>.Instance;
        if (manager?.m_WorkerDataList == null || manager.m_WorkerDataList.Count <= 0)
        {
            return false;
        }

        return true;
    }

    internal static bool IsModEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        return options.EnableMod && GetRuntimeMode() != RuntimeMode.Disabled;
    }

    internal static bool IsArcUiEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        RuntimeMode mode = GetRuntimeMode();
        return options.EnableMod
            && (mode == RuntimeMode.Full ? options.EnableArcUi : mode == RuntimeMode.UiOnly);
    }

    internal static bool IsArcAssetLoaderEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        RuntimeMode mode = GetRuntimeMode();
        return options.EnableMod
            && (mode == RuntimeMode.Full ? options.EnableArcAssetLoader : mode == RuntimeMode.UiOnly);
    }

    internal static bool IsPhoneOverhaulHooksEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        RuntimeMode mode = GetRuntimeMode();
        return options.EnableMod
            && (mode == RuntimeMode.Full ? options.EnablePhoneOverhaulHooks : mode == RuntimeMode.UiOnly);
    }

    internal static bool IsRosterExtensionEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        RuntimeMode mode = GetRuntimeMode();
        return options.EnableMod
            && (mode == RuntimeMode.Full ? options.EnableRosterExtension : mode == RuntimeMode.RosterOnly);
    }

    internal static bool IsWorkerRuntimePatchEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        RuntimeMode mode = GetRuntimeMode();
        return options.EnableMod
            && (mode == RuntimeMode.Full ? options.EnableWorkerRuntimePatch : mode == RuntimeMode.AppearanceOnly);
    }

    internal static bool IsNpcMutatorEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        RuntimeMode mode = GetRuntimeMode();
        return options.EnableMod
            && (mode == RuntimeMode.Full ? options.EnableNpcMutator : mode == RuntimeMode.AppearanceOnly);
    }

    internal static bool IsCustomerAppearancePatchEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        RuntimeMode mode = GetRuntimeMode();
        return options.EnableMod
            && (mode == RuntimeMode.Full ? options.EnableCustomerAppearancePatch : mode == RuntimeMode.AppearanceOnly);
    }

    internal static bool IsWorkerAppearancePatchEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        RuntimeMode mode = GetRuntimeMode();
        return options.EnableMod
            && (mode == RuntimeMode.Full ? options.EnableWorkerAppearancePatch : mode == RuntimeMode.AppearanceOnly);
    }

    internal static bool IsWorkerModelValidationEnabled()
    {
        AppearanceShuffleOptions options = AppearanceSettingsHelper.GetCurrentOptions();
        RuntimeMode mode = GetRuntimeMode();
        return options.EnableMod
            && (mode == RuntimeMode.Full ? options.EnableGeneratedWorkerModelValidation : mode == RuntimeMode.AppearanceOnly);
    }

    internal static bool TripIfDisabled(bool enabled, string subsystemName, ManualLogSource? logger)
    {
        if (!enabled)
        {
            logger?.LogError($"Custom Workers kill-switch tripwire: subsystem '{subsystemName}' was invoked while disabled.");
            return true;
        }

        return false;
    }
}

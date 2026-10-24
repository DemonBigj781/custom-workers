using System;
using System.Reflection;
using Xunit;

namespace CustomWorkers.Tests;

public sealed class KillSwitchHelperTests
{
    private static readonly FieldInfo CurrentOptionsField = typeof(AppearanceSettingsHelper)
        .GetField("currentOptions", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Could not find AppearanceSettingsHelper.currentOptions.");

    [Fact]
    public void UiOnlyMode_RespectsIndividualUiSwitches()
    {
        AppearanceShuffleOptions original = AppearanceSettingsHelper.GetCurrentOptions();

        try
        {
            SetCurrentOptions(new AppearanceShuffleOptions
            {
                RuntimeMode = KillSwitchHelper.RuntimeMode.UiOnly,
                EnableMod = true,
                EnableArcUi = true,
                EnableArcAssetLoader = false,
                EnablePhoneOverhaulHooks = false
            });

            Assert.True(KillSwitchHelper.IsArcUiEnabled());
            Assert.False(KillSwitchHelper.IsArcAssetLoaderEnabled());
            Assert.False(KillSwitchHelper.IsPhoneOverhaulHooksEnabled());
        }
        finally
        {
            SetCurrentOptions(original);
        }
    }

    [Fact]
    public void AppearanceOnlyMode_DoesNotSilentlyEnableSiblingSwitches()
    {
        AppearanceShuffleOptions original = AppearanceSettingsHelper.GetCurrentOptions();

        try
        {
            SetCurrentOptions(new AppearanceShuffleOptions
            {
                RuntimeMode = KillSwitchHelper.RuntimeMode.AppearanceOnly,
                EnableMod = true,
                EnableWorkerRuntimePatch = true,
                EnableNpcMutator = false,
                EnableCustomerAppearancePatch = false,
                EnableWorkerAppearancePatch = false,
                EnableGeneratedWorkerModelValidation = false
            });

            Assert.True(KillSwitchHelper.IsWorkerRuntimePatchEnabled());
            Assert.False(KillSwitchHelper.IsNpcMutatorEnabled());
            Assert.False(KillSwitchHelper.IsCustomerAppearancePatchEnabled());
            Assert.False(KillSwitchHelper.IsWorkerAppearancePatchEnabled());
            Assert.False(KillSwitchHelper.IsWorkerModelValidationEnabled());
        }
        finally
        {
            SetCurrentOptions(original);
        }
    }

    [Fact]
    public void SaveBackedWorkerState_AllowsFreshWorldWithNoSavedWorkers()
    {
        bool[] hiredFlags = new bool[] { false, false, false };

        Assert.True(KillSwitchHelper.IsSaveBackedWorkerStateAligned(hiredFlags, runtimeWorkerCount: 3, restPointCount: 3, workerSaveDataCount: 0));
    }

    [Fact]
    public void SaveBackedWorkerState_BlocksWhenMissingSaveEntryForHiredWorker()
    {
        bool[] hiredFlags = new bool[] { true, false, false };

        Assert.False(KillSwitchHelper.IsSaveBackedWorkerStateAligned(hiredFlags, runtimeWorkerCount: 3, restPointCount: 3, workerSaveDataCount: 0));
    }

    private static void SetCurrentOptions(AppearanceShuffleOptions options)
    {
        CurrentOptionsField.SetValue(null, options);
    }
}

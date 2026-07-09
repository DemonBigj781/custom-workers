using BepInEx.Configuration;
using UnityEngine;

namespace CustomWorkers;

internal static class AppearanceSettingsHelper
{
    private static AppearanceShuffleOptions currentOptions = new AppearanceShuffleOptions();
    private static ConfigEntry<KillSwitchHelper.RuntimeMode>? runtimeMode;
    private static ConfigEntry<bool>? enableMod;
    private static ConfigEntry<bool>? enableArcUi;
    private static ConfigEntry<bool>? enableArcAssetLoader;
    private static ConfigEntry<bool>? enablePhoneOverhaulHooks;
    private static ConfigEntry<bool>? enableRosterExtension;
    private static ConfigEntry<bool>? enableWorkerRuntimePatch;
    private static ConfigEntry<bool>? enableNpcMutator;
    private static ConfigEntry<bool>? customerShirt;
    private static ConfigEntry<bool>? enableCustomerAppearancePatch;
    private static ConfigEntry<bool>? customerPants;
    private static ConfigEntry<bool>? customerShoes;
    private static ConfigEntry<bool>? customerHair;
    private static ConfigEntry<bool>? workerShirt;
    private static ConfigEntry<bool>? enableWorkerAppearancePatch;
    private static ConfigEntry<bool>? workerPants;
    private static ConfigEntry<bool>? workerShoes;
    private static ConfigEntry<bool>? workerHair;
    private static ConfigEntry<AppearanceSkinMode>? customerSkinMode;
    private static ConfigEntry<AppearanceGenderFilter>? customerSkinGenderFilter;
    private static ConfigEntry<AppearanceSkinMode>? workerSkinMode;
    private static ConfigEntry<AppearanceGenderFilter>? workerSkinGenderFilter;
    private static ConfigEntry<AppearanceSkinMode>? playerSkinMode;
    private static ConfigEntry<AppearanceGenderFilter>? playerSkinGenderFilter;
    private static ConfigEntry<bool>? enableGeneratedWorkerModelValidation;
    private static ConfigEntry<bool>? mrBurnsMode;
    private static ConfigEntry<bool>? debugMapPopulationEvery10Seconds;
    private static ConfigEntry<int>? generatedWorkerCount;
    private static ConfigEntry<string>? exportDebugSnapshotKey;
    private static ConfigEntry<string>? exportPhoneScreenshotKey;
    private static ConfigEntry<WorkerIconSourceMode>? workerIconSourceMode;
    private static ConfigEntry<string>? workerIconBase64;
    private static string configFilePath = string.Empty;

    internal static void Configure(ConfigFile config)
    {
        configFilePath = config.ConfigFilePath;
        runtimeMode = config.Bind("General", "RuntimeMode", KillSwitchHelper.RuntimeMode.Full, "Top-level runtime gate. Disabled stops the mod after startup diagnostics. DiagnosticsOnly avoids gameplay and UI mutations. Other modes enable only their named subsystem families.");
        enableMod = config.Bind("General", "EnableMod", true, "Master kill switch for Custom Workers. When false, runtime patches should early-out and only startup diagnostics remain.");
        enableArcUi = config.Bind("General", "EnableArcUi", true, "Enable ARC Recruiter phone app, tile binding, and cloned UI behavior.");
        enableArcAssetLoader = config.Bind("General", "EnableArcAssetLoader", true, "Enable ARC Recruiter embedded asset preload and assignment logic.");
        enablePhoneOverhaulHooks = config.Bind("General", "EnablePhoneOverhaulHooks", true, "Enable Phone Overhaul registration and log-only hook installation for ARC Recruiter.");
        enableRosterExtension = config.Bind("General", "EnableRosterExtension", true, "Enable worker roster extension and generated worker insertion.");
        enableWorkerRuntimePatch = config.Bind("General", "EnableWorkerRuntimePatch", true, "Enable generated worker runtime initialization patching.");
        enableNpcMutator = config.Bind("General", "EnableNpcMutator", true, "Enable NPC model/body mutator logic. When false, generated workers fall back to the first worker asset template.");
        enableCustomerAppearancePatch = config.Bind("Appearance.Customers", "EnablePatch", false, "Enable customer appearance patching. Disable for load/save isolation.");
        customerShirt = config.Bind("Appearance.Customers", "ShuffleShirt", true, "Shuffle customer shirt colors.");
        customerPants = config.Bind("Appearance.Customers", "ShufflePants", true, "Shuffle customer pants colors.");
        customerShoes = config.Bind("Appearance.Customers", "ShuffleShoes", true, "Shuffle customer shoe colors.");
        customerHair = config.Bind("Appearance.Customers", "ShuffleHair", true, "Shuffle customer hair colors.");

        enableWorkerAppearancePatch = config.Bind("Appearance.Workers", "EnablePatch", false, "Enable generated worker appearance color patching. Disable for load/save isolation.");
        workerShirt = config.Bind("Appearance.Workers", "ShuffleShirt", true, "Shuffle generated worker shirt colors.");
        workerPants = config.Bind("Appearance.Workers", "ShufflePants", true, "Shuffle generated worker pants colors.");
        workerShoes = config.Bind("Appearance.Workers", "ShuffleShoes", true, "Shuffle generated worker shoe colors.");
        workerHair = config.Bind("Appearance.Workers", "ShuffleHair", true, "Shuffle generated worker hair colors.");
        customerSkinMode = config.Bind("Appearance.Customers", "SkinMode", AppearanceSkinMode.Off, "Off, Simpsons, or Random customer skin tones.");
        customerSkinGenderFilter = config.Bind("Appearance.Customers", "SkinGenderFilter", AppearanceGenderFilter.Both, "Apply customer skin mode to boys only, girls only, or both.");
        workerSkinMode = config.Bind("Appearance.Workers", "SkinMode", AppearanceSkinMode.Off, "Off, Simpsons, or Random generated worker skin tones.");
        workerSkinGenderFilter = config.Bind("Appearance.Workers", "SkinGenderFilter", AppearanceGenderFilter.Both, "Apply worker skin mode to boys only, girls only, or both.");
        enableGeneratedWorkerModelValidation = config.Bind("Appearance.Workers", "EnableGeneratedWorkerModelValidation", false, "Enable runtime learning/validation of generated worker model index to body-type mappings.");
        playerSkinMode = config.Bind("Appearance.Player", "SkinMode", AppearanceSkinMode.Off, "Reserved for player/multiplayer skin overrides.");
        playerSkinGenderFilter = config.Bind("Appearance.Player", "SkinGenderFilter", AppearanceGenderFilter.Both, "Reserved for player/multiplayer skin gender filtering.");
        generatedWorkerCount = config.Bind("Workers.Generation", "GeneratedWorkerCount", 7, "How many generated workers to add. Clamped to the maximum available unique first/last name pairs.");
        mrBurnsMode = config.Bind("Economy", "MrBurnsMode", false, "Makes all employees free to recruit at level 0 and removes daily rent, electricity, and salary costs.");
        debugMapPopulationEvery10Seconds = config.Bind("Debug", "LogMapPopulationEvery10Seconds", false, "Logs active worker and customer population every 10 seconds for debugging.");
        exportDebugSnapshotKey = config.Bind("Debug", "ExportDebugSnapshotKey", "Plus", "Keyboard key name used to export debug snapshots on demand. Example values: Plus, KeypadPlus, F8.");
        exportPhoneScreenshotKey = config.Bind("Debug", "ExportPhoneScreenshotKey", "Minus", "Keyboard key name used to capture a screenshot and export current phone debug artifacts. Example values: Minus, KeypadMinus, F9.");
        workerIconSourceMode = config.Bind("Appearance.Workers", "WorkerIconSourceMode", WorkerIconSourceMode.File, "Choose whether the generated worker icon loads from file or base64 text.");
        workerIconBase64 = config.Bind("Appearance.Workers", "WorkerIconBase64", string.Empty, "Optional base64 PNG source for generated worker icons when WorkerIconSourceMode is Base64.");

        UpdateCurrentOptions();
        runtimeMode.SettingChanged += (_, _) => UpdateCurrentOptions();
        enableMod.SettingChanged += (_, _) => UpdateCurrentOptions();
        enableArcUi.SettingChanged += (_, _) => UpdateCurrentOptions();
        enableArcAssetLoader.SettingChanged += (_, _) => UpdateCurrentOptions();
        enablePhoneOverhaulHooks.SettingChanged += (_, _) => UpdateCurrentOptions();
        enableRosterExtension.SettingChanged += (_, _) => UpdateCurrentOptions();
        enableWorkerRuntimePatch.SettingChanged += (_, _) => UpdateCurrentOptions();
        enableNpcMutator.SettingChanged += (_, _) => UpdateCurrentOptions();
        enableCustomerAppearancePatch.SettingChanged += (_, _) => UpdateCurrentOptions();
        customerShirt.SettingChanged += (_, _) => UpdateCurrentOptions();
        customerPants.SettingChanged += (_, _) => UpdateCurrentOptions();
        customerShoes.SettingChanged += (_, _) => UpdateCurrentOptions();
        customerHair.SettingChanged += (_, _) => UpdateCurrentOptions();
        enableWorkerAppearancePatch.SettingChanged += (_, _) => UpdateCurrentOptions();
        workerShirt.SettingChanged += (_, _) => UpdateCurrentOptions();
        workerPants.SettingChanged += (_, _) => UpdateCurrentOptions();
        workerShoes.SettingChanged += (_, _) => UpdateCurrentOptions();
        workerHair.SettingChanged += (_, _) => UpdateCurrentOptions();
        customerSkinMode.SettingChanged += (_, _) => UpdateCurrentOptions();
        customerSkinGenderFilter.SettingChanged += (_, _) => UpdateCurrentOptions();
        workerSkinMode.SettingChanged += (_, _) => UpdateCurrentOptions();
        workerSkinGenderFilter.SettingChanged += (_, _) => UpdateCurrentOptions();
        enableGeneratedWorkerModelValidation.SettingChanged += (_, _) => UpdateCurrentOptions();
        playerSkinMode.SettingChanged += (_, _) => UpdateCurrentOptions();
        playerSkinGenderFilter.SettingChanged += (_, _) => UpdateCurrentOptions();
        generatedWorkerCount.SettingChanged += (_, _) => UpdateCurrentOptions();
        mrBurnsMode.SettingChanged += (_, _) => UpdateCurrentOptions();
        debugMapPopulationEvery10Seconds.SettingChanged += (_, _) => UpdateCurrentOptions();
        workerIconSourceMode.SettingChanged += (_, _) => WorkerIconRules.ResetCache();
        workerIconBase64.SettingChanged += (_, _) => WorkerIconRules.ResetCache();
    }

    internal static AppearanceShuffleOptions GetCurrentOptions()
    {
        return currentOptions;
    }

    internal static string GetConfigFilePath()
    {
        return configFilePath;
    }

    internal static WorkerIconSourceMode GetWorkerIconSourceMode()
    {
        return workerIconSourceMode?.Value ?? WorkerIconSourceMode.File;
    }

    internal static string GetWorkerIconBase64()
    {
        return workerIconBase64?.Value ?? string.Empty;
    }

    internal static KeyCode GetExportDebugSnapshotKey()
    {
        return ParseKeyCode(exportDebugSnapshotKey?.Value, KeyCode.Plus);
    }

    internal static KeyCode GetExportPhoneScreenshotKey()
    {
        return ParseKeyCode(exportPhoneScreenshotKey?.Value, KeyCode.Minus);
    }

    private static void UpdateCurrentOptions()
    {
        currentOptions = new AppearanceShuffleOptions
        {
            RuntimeMode = runtimeMode?.Value ?? KillSwitchHelper.RuntimeMode.Full,
            EnableMod = enableMod?.Value ?? true,
            EnableArcUi = enableArcUi?.Value ?? true,
            EnableArcAssetLoader = enableArcAssetLoader?.Value ?? true,
            EnablePhoneOverhaulHooks = enablePhoneOverhaulHooks?.Value ?? true,
            EnableRosterExtension = enableRosterExtension?.Value ?? true,
            EnableWorkerRuntimePatch = enableWorkerRuntimePatch?.Value ?? true,
            EnableNpcMutator = enableNpcMutator?.Value ?? true,
            EnableCustomerAppearancePatch = enableCustomerAppearancePatch?.Value ?? false,
            EnableWorkerAppearancePatch = enableWorkerAppearancePatch?.Value ?? false,
            EnableGeneratedWorkerModelValidation = enableGeneratedWorkerModelValidation?.Value ?? false,
            CustomerShirt = customerShirt?.Value ?? true,
            CustomerPants = customerPants?.Value ?? true,
            CustomerShoes = customerShoes?.Value ?? true,
            CustomerHair = customerHair?.Value ?? true,
            WorkerShirt = workerShirt?.Value ?? true,
            WorkerPants = workerPants?.Value ?? true,
            WorkerShoes = workerShoes?.Value ?? true,
            WorkerHair = workerHair?.Value ?? true,
            CustomerSkinMode = customerSkinMode?.Value ?? AppearanceSkinMode.Off,
            CustomerSkinGenderFilter = customerSkinGenderFilter?.Value ?? AppearanceGenderFilter.Both,
            WorkerSkinMode = workerSkinMode?.Value ?? AppearanceSkinMode.Off,
            WorkerSkinGenderFilter = workerSkinGenderFilter?.Value ?? AppearanceGenderFilter.Both,
            PlayerSkinMode = playerSkinMode?.Value ?? AppearanceSkinMode.Off,
            PlayerSkinGenderFilter = playerSkinGenderFilter?.Value ?? AppearanceGenderFilter.Both,
            GeneratedWorkerCount = generatedWorkerCount?.Value ?? 7,
            MrBurnsMode = mrBurnsMode?.Value ?? false,
            DebugMapPopulationEvery10Seconds = debugMapPopulationEvery10Seconds?.Value ?? false
        };
    }

    internal static string DescribeCurrentOptions()
    {
        AppearanceShuffleOptions options = currentOptions;
        return $"runtimeMode={options.RuntimeMode},enableMod={options.EnableMod},arcUi={options.EnableArcUi},arcAssetLoader={options.EnableArcAssetLoader},phoneOverhaulHooks={options.EnablePhoneOverhaulHooks},rosterExtension={options.EnableRosterExtension},workerRuntime={options.EnableWorkerRuntimePatch},npcMutator={options.EnableNpcMutator},customerPatch={options.EnableCustomerAppearancePatch},workerPatch={options.EnableWorkerAppearancePatch},workerModelValidation={options.EnableGeneratedWorkerModelValidation},customerSkin={options.CustomerSkinMode}/{options.CustomerSkinGenderFilter},workerSkin={options.WorkerSkinMode}/{options.WorkerSkinGenderFilter},playerSkin={options.PlayerSkinMode}/{options.PlayerSkinGenderFilter},generatedWorkerCount={options.GeneratedWorkerCount},mrBurns={options.MrBurnsMode},debugPopulation={options.DebugMapPopulationEvery10Seconds}";
    }

    private static KeyCode ParseKeyCode(string? value, KeyCode fallback)
    {
        if (System.Enum.TryParse(value ?? string.Empty, ignoreCase: true, out KeyCode keyCode))
        {
            return keyCode;
        }

        return fallback;
    }
}

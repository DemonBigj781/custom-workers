using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using I2.Loc;
using UnityEngine;
using UnityEngine.UI;
using CC;

namespace CustomWorkers;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("munch.PhoneOverhaul")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "jack.tcgsim.custom-workers";
    public const string PluginName = "Custom Workers";
    public const string PluginVersion = "0.2.0";

    private static readonly Harmony HarmonyInstance = new Harmony(PluginGuid);
    private static readonly FieldInfo ApparelObjectsField = AccessTools.Field(typeof(CharacterCustomization), "ApparelObjects");
    private static readonly FieldInfo SpawnedWorkerCountField = AccessTools.Field(typeof(WorkerManager), "m_SpawnedWorkerCount");
    private static readonly MethodInfo AddWorkerPrefabMethod = AccessTools.Method(typeof(WorkerManager), "AddWorkerPrefab");
    private static readonly FieldInfo MaxMaleModelIndexField = AccessTools.Field(typeof(CustomerManager), "m_MaxMaleModelIndex");
    private static readonly FieldInfo MaxFemaleModelIndexField = AccessTools.Field(typeof(CustomerManager), "m_MaxFemaleModelIndex");
    private static readonly FieldInfo WorkerCardPackItemTypeListField = AccessTools.Field(typeof(Worker), "m_CardPackItemTypeList");
    private static readonly FieldInfo WorkerCardPackItemTypeEnabledListField = AccessTools.Field(typeof(Worker), "m_CardPackItemTypeEnabledList");
    private static readonly FieldInfo CustomerListField = AccessTools.Field(typeof(CustomerManager), "m_CustomerList");
    private static readonly FieldInfo ScrollEndPosParentField = AccessTools.Field(typeof(GenericSliderScreen), "m_ScrollEndPosParent");
    private static readonly FieldInfo MaxPosXField = AccessTools.Field(typeof(GenericSliderScreen), "m_MaxPosX");
    private static readonly FieldInfo CanEvaluateMaxScrollPosField = AccessTools.Field(typeof(GenericSliderScreen), "m_CanEvaluateMaxScrollPos");
    private static readonly FieldInfo MaxPosFoundField = AccessTools.Field(typeof(GenericSliderScreen), "m_MaxPosFound");
    private static readonly FieldInfo MaxPosAccurateFoundField = AccessTools.Field(typeof(GenericSliderScreen), "m_MaxPosAccurateFound");
    private static readonly FieldInfo PosXField = AccessTools.Field(typeof(GenericSliderScreen), "m_PosX");
    private static readonly FieldInfo LerpPosXField = AccessTools.Field(typeof(GenericSliderScreen), "m_LerpPosX");
    private static readonly FieldInfo HireWorkerPanelLevelRequiredField = AccessTools.Field(typeof(HireWorkerPanelUI), "m_LevelRequired");
    private static readonly FieldInfo HireWorkerPanelTotalHireFeeField = AccessTools.Field(typeof(HireWorkerPanelUI), "m_TotalHireFee");
    private static readonly FieldInfo HireWorkerPanelIndexField = AccessTools.Field(typeof(HireWorkerPanelUI), "m_Index");
    private static readonly FieldInfo WorkerInteractSalaryCostField = AccessTools.Field(typeof(WorkerInteractUIScreen), "m_SalaryCost");
    private static readonly MethodInfo EvaluateScrollerMethod = AccessTools.Method(typeof(GenericSliderScreen), "EvaluateActiveRestockUIScroller");
    private static Plugin? Instance;
    private static bool workerRosterExtended;
    private static bool hirePanelsExpandedThisInit;
    private static int customerRosterEventSequence;
    private static float lastManualSnapshotRealtime;
    private static List<WorkerData>? originalWorkerDataListForVanillaHireScreen;
    private static List<WorkerData>? canonicalExpandedWorkerDataList;
    private static readonly Dictionary<int, GeneratedWorkerAppearance> GeneratedWorkerAppearances = new Dictionary<int, GeneratedWorkerAppearance>();

    private static bool IsModEnabled()
    {
        return KillSwitchHelper.IsModEnabled();
    }

    private static bool IsArcUiEnabled()
    {
        return KillSwitchHelper.IsArcUiEnabled();
    }

    private static bool IsArcAssetLoaderEnabled()
    {
        return KillSwitchHelper.IsArcAssetLoaderEnabled();
    }

    private static bool IsPhoneOverhaulHooksEnabled()
    {
        return KillSwitchHelper.IsPhoneOverhaulHooksEnabled();
    }

    private static bool IsRosterExtensionEnabled()
    {
        return KillSwitchHelper.IsRosterExtensionEnabled();
    }

    private static bool IsWorkerRuntimePatchEnabled()
    {
        return KillSwitchHelper.IsWorkerRuntimePatchEnabled();
    }

    private static bool IsNpcMutatorEnabled()
    {
        return KillSwitchHelper.IsNpcMutatorEnabled();
    }

    private void Awake()
    {
        Instance = this;
        LogHelper.SetRuntimeLogger(Logger);
        AppearanceSettingsHelper.Configure(Config);
        Logger.LogInfo(GetBuildStamp());
        Logger.LogInfo(SettingsHelper.GetConfigStamp());
        Logger.LogInfo(SettingsHelper.GetRunHeaderLine());
        Logger.LogInfo(KillSwitchSettingsHelper.GetKillSwitchSummary());
        SettingsHelper.WriteDirectRunHeader(Logger);
        Logger.LogInfo($"Custom Workers settings snapshot: {AppearanceSettingsHelper.DescribeCurrentOptions()}");
        Logger.LogInfo($"Custom Workers runtime mode: {KillSwitchHelper.GetRuntimeMode()}");
        if (!IsModEnabled())
        {
            Logger.LogWarning("Custom Workers master kill switch is disabled. Skipping registration, patching, and runtime hooks.");
            return;
        }

        Logger.LogInfo($"Custom Workers startup checkpoint: WorkerManager={(CSingleton<WorkerManager>.Instance != null ? "present" : "null")} rosterCount={CSingleton<WorkerManager>.Instance?.m_WorkerDataList?.Count ?? -1}");
        if (IsArcAssetLoaderEnabled())
        {
            ArcRecruiterAssetLayerHelper.PreloadCoreAssets(Logger);
        }
        else
        {
            Logger.LogWarning("Custom Workers ARC asset loader subsystem disabled. Skipping embedded ARC asset preload.");
        }

        if (IsArcUiEnabled() && IsPhoneOverhaulHooksEnabled())
        {
            PhoneOverhaulAppHelper.EnsureRegistered(Logger);
            Logger.LogInfo("Custom Workers invoking Phone Overhaul debug hook installer.");
            PhoneOverhaulDebugHookHelper.Install(HarmonyInstance, Logger);
            Logger.LogInfo("Custom Workers returned from Phone Overhaul debug hook installer.");
        }
        else
        {
            Logger.LogWarning("Custom Workers ARC UI or Phone Overhaul hook subsystem disabled. Skipping ARC registration and Phone Overhaul hook installation.");
        }
        bool patchAllSucceeded = false;
        if (KillSwitchHelper.GetRuntimeMode() == KillSwitchHelper.RuntimeMode.DiagnosticsOnly)
        {
            Logger.LogWarning("Custom Workers runtime mode is DiagnosticsOnly. Skipping Harmony PatchAll and leaving the run diagnostics-only.");
        }
        else
        {
            try
            {
                Logger.LogInfo("Custom Workers starting Harmony PatchAll for plugin assembly.");
                HarmonyInstance.PatchAll(typeof(Plugin).Assembly);
                patchAllSucceeded = true;
                Logger.LogInfo("Custom Workers completed Harmony PatchAll for plugin assembly.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Custom Workers PatchAll failed; continuing with degraded diagnostics: {ex}");
            }
            finally
            {
                if (IsArcUiEnabled() && IsPhoneOverhaulHooksEnabled())
                {
                    PhoneOverhaulAppHelper.EnsureRegistered(Logger);
                }
                Logger.LogInfo($"Custom Workers startup diagnostics installed. patchAllSucceeded={patchAllSucceeded} hookStatus={PhoneOverhaulDebugHookHelper.GetStatusSummary()}.");
            }
        }

        if (KillSwitchHelper.GetRuntimeMode() == KillSwitchHelper.RuntimeMode.DiagnosticsOnly)
        {
            Logger.LogInfo($"Custom Workers startup diagnostics installed. patchAllSucceeded={patchAllSucceeded} hookStatus={PhoneOverhaulDebugHookHelper.GetStatusSummary()}.");
        }

        if (AppearanceSettingsHelper.GetCurrentOptions().DebugMapPopulationEvery10Seconds)
        {
            StartCoroutine(MapPopulationDebugLoop());
        }
        else
        {
            Logger.LogInfo("Custom Workers skipped map population loop because debug population logging is disabled.");
        }
        Logger.LogInfo("Custom Workers 0.2.0 loaded. NPC clothing colors + worker roster extension support.");
    }

    private static string GetBuildStamp()
    {
        string assemblyPath = typeof(Plugin).Assembly.Location;
        string version = typeof(Plugin).Assembly.GetName().Version?.ToString() ?? PluginVersion;
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
        {
            return $"Custom Workers build stamp: attempt={BuildInfo.BuildAttempt} compileUtc={BuildInfo.BuildTimestampUtc} compileLocal={BuildInfo.BuildTimestampLocal} configuration={BuildInfo.BuildConfiguration} path=<unknown> version={version}";
        }

        FileInfo fileInfo = new FileInfo(assemblyPath);
        string sha256Prefix;
        using (var stream = File.OpenRead(assemblyPath))
        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(stream);
            sha256Prefix = BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0, 12);
        }

        return $"Custom Workers build stamp: attempt={BuildInfo.BuildAttempt} compileUtc={BuildInfo.BuildTimestampUtc} compileLocal={BuildInfo.BuildTimestampLocal} configuration={BuildInfo.BuildConfiguration} path={assemblyPath} version={version} fileTimestamp={fileInfo.LastWriteTime:O} size={fileInfo.Length} sha256={sha256Prefix}";
    }

    private static string GetTransformPath(Transform transform)
    {
        var segments = new Stack<string>();
        Transform? current = transform;
        while (current != null)
        {
            segments.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", segments.ToArray());
    }

    private void OnDestroy()
    {
        HarmonyInstance.UnpatchSelf();
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!IsModEnabled())
        {
            return;
        }

        if (InputHelper.IsKeyPressed(AppearanceSettingsHelper.GetExportPhoneScreenshotKey()))
        {
            TriggerManualExport(includeScreenshot: true, AppearanceSettingsHelper.GetExportPhoneScreenshotKey());
            return;
        }

        if (!InputHelper.IsKeyPressed(AppearanceSettingsHelper.GetExportDebugSnapshotKey()))
        {
            return;
        }

        TriggerManualExport(includeScreenshot: false, AppearanceSettingsHelper.GetExportDebugSnapshotKey());
    }

    private void TriggerManualExport(bool includeScreenshot, KeyCode key)
    {
        if (Time.realtimeSinceStartup - lastManualSnapshotRealtime < 0.5f)
        {
            return;
        }

        lastManualSnapshotRealtime = Time.realtimeSinceStartup;
        string snapshotName = includeScreenshot ? "manual-screenshot" : "manual";
        Logger.LogInfo($"Custom Workers manual export triggered by key {key} (screenshot={includeScreenshot}).");
        if (includeScreenshot)
        {
            LogHelper.CaptureScreenshot(Logger, snapshotName);
        }

        HireScreenCloneHelper.ExportCurrentDebugSnapshots(Logger, snapshotName);
    }

    private IEnumerator MapPopulationDebugLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (!IsModEnabled() || !AppearanceSettingsHelper.GetCurrentOptions().DebugMapPopulationEvery10Seconds)
            {
                continue;
            }

            LogCurrentMapPopulation();
        }
    }

    [HarmonyPatch(typeof(Worker), "InitializeCharacter")]
    private static class WorkerInitializeCharacterPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsWorkerRuntimePatchEnabled();
        }

        private static bool Prefix(Worker __instance)
        {
            if (!IsWorkerRuntimePatchEnabled() || __instance == null)
            {
                return true;
            }

            Worker nonNullWorker = __instance;
            Instance?.Logger.LogInfo($"Custom Workers Worker.InitializeCharacter prefix: workerIndex={nonNullWorker.m_WorkerIndex} isFemale={nonNullWorker.m_IsFemale} generated={WorkerHelper.TryGetGeneratedWorkerAppearance(nonNullWorker, GeneratedWorkerAppearances, out _)} hasCustomization={nonNullWorker.m_CharacterCustom != null}");
            if (!WorkerHelper.TryGetGeneratedWorkerAppearance(nonNullWorker, GeneratedWorkerAppearances, out GeneratedWorkerAppearance? appearance))
            {
                return true;
            }

            GeneratedWorkerAppearance generatedAppearance = appearance!;
            nonNullWorker.m_IsFemale = generatedAppearance.IsFemale;
            CharacterCustomization? customization = nonNullWorker.m_CharacterCustom;
            if (!GeneratedWorkerAppearanceRules.ShouldApplyGeneratedCharacter(customization != null))
            {
                return false;
            }

            CharacterCustomization nonNullCustomization = customization!;
            if (IsNpcMutatorEnabled())
            {
                nonNullCustomization.CharacterName = GeneratedWorkerAppearanceRules.GetCharacterName(generatedAppearance.IsFemale, generatedAppearance.CharacterModelIndex);
            }
            else
            {
                nonNullCustomization.CharacterName = WorkerHelper.GetFallbackWorkerCharacterName();
                Instance?.Logger.LogInfo($"Custom Workers NPC mutator disabled: generated worker {nonNullWorker.m_WorkerIndex} falling back to {nonNullCustomization.CharacterName}.");
            }
            nonNullCustomization.Initialize();
            IList? cardPackItemTypeList = WorkerCardPackItemTypeListField?.GetValue(nonNullWorker) as IList;
            IList? cardPackItemTypeEnabledList = WorkerCardPackItemTypeEnabledListField?.GetValue(nonNullWorker) as IList;
            if (cardPackItemTypeList?.Count == 0 && cardPackItemTypeEnabledList?.Count == 0)
            {
                for (int i = 0; i < CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_CardPackItemTypeList.Count; i++)
                {
                    cardPackItemTypeList?.Add(CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_CardPackItemTypeList[i]);
                    cardPackItemTypeEnabledList?.Add(true);
                }
            }

            if (nonNullWorker.m_TaskLevel.Count == 0 && nonNullWorker.m_ExpList.Count == 0)
            {
                for (int j = 0; j < 100; j++)
                {
                    nonNullWorker.m_TaskLevel.Add(0);
                    nonNullWorker.m_ExpList.Add(0);
                }
            }

            return false;
        }

        private static void Postfix(Worker __instance)
        {
            if (!IsWorkerRuntimePatchEnabled())
            {
                return;
            }

            if (AppearanceSettingsHelper.GetCurrentOptions().EnableGeneratedWorkerModelValidation)
            {
                WorkerHelper.ObserveGeneratedWorkerModelResult(__instance, GeneratedWorkerAppearances);
            }
            string observedBodyType = __instance?.m_CharacterCustom?.StoredCharacterData?.CharacterPrefab ?? "<none>";
            Instance?.Logger.LogInfo($"Custom Workers Worker.InitializeCharacter postfix: workerIndex={__instance?.m_WorkerIndex ?? -1} isFemale={__instance?.m_IsFemale} characterName={__instance?.m_CharacterCustom?.CharacterName ?? "<none>"} bodyType={observedBodyType}");
            WorkerHelper.ApplyGeneratedWorkerClothingColors(__instance, ApparelObjectsField, GeneratedWorkerAppearances, AppearanceSettingsHelper.GetCurrentOptions());
        }
    }

    [HarmonyPatch(typeof(Customer), "RandomizeCharacterMesh")]
    private static class CustomerRandomizeCharacterMeshPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsCustomerAppearancePatchEnabled();
        }

        private static void Prefix(Customer __instance)
        {
            if (!AppearanceSettingsHelper.GetCurrentOptions().EnableCustomerAppearancePatch)
            {
                return;
            }

            Instance?.Logger.LogInfo($"Custom Workers Customer.RandomizeCharacterMesh prefix: customerId={__instance?.gameObject.GetInstanceID() ?? -1} isFemale={__instance?.m_IsFemale} currentCharacterName={__instance?.m_CharacterCustom?.CharacterName ?? "<none>"} hasInit={__instance?.m_CharacterCustom?.m_HasInit}");
        }

        private static void Postfix(Customer __instance)
        {
            if (!AppearanceSettingsHelper.GetCurrentOptions().EnableCustomerAppearancePatch)
            {
                return;
            }

            string observedBodyType = __instance?.m_CharacterCustom?.StoredCharacterData?.CharacterPrefab ?? "<none>";
            Instance?.Logger.LogInfo($"Custom Workers Customer.RandomizeCharacterMesh postfix: customerId={__instance?.gameObject.GetInstanceID() ?? -1} isFemale={__instance?.m_IsFemale} characterName={__instance?.m_CharacterCustom?.CharacterName ?? "<none>"} bodyType={observedBodyType}");
            AppearanceHelper.ApplyCustomerAppearanceShuffling(__instance, ApparelObjectsField, AppearanceSettingsHelper.GetCurrentOptions());
        }
    }

    [HarmonyPatch(typeof(Customer), "ActivateCustomer")]
    private static class CustomerActivatePatch
    {
        private static bool Prepare()
        {
            return AppearanceSettingsHelper.GetCurrentOptions().DebugMapPopulationEvery10Seconds;
        }

        private static void Postfix(Customer __instance)
        {
            if (!AppearanceSettingsHelper.GetCurrentOptions().DebugMapPopulationEvery10Seconds)
            {
                return;
            }

            Instance?.WriteCustomerRosterEventSnapshot("enter", __instance);
        }
    }

    [HarmonyPatch(typeof(Customer), "DeactivateCustomer")]
    private static class CustomerDeactivatePatch
    {
        private static bool Prepare()
        {
            return AppearanceSettingsHelper.GetCurrentOptions().DebugMapPopulationEvery10Seconds;
        }

        private static void Prefix(Customer __instance)
        {
            if (!AppearanceSettingsHelper.GetCurrentOptions().DebugMapPopulationEvery10Seconds)
            {
                return;
            }

            Instance?.WriteCustomerRosterEventSnapshot("leave", __instance);
        }
    }

    [HarmonyPatch(typeof(WorkerData), "GetSalaryCostText")]
    private static class WorkerDataSalaryCostPatch
    {
        private static bool Prepare()
        {
            return AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode;
        }

        private static void Postfix(ref string __result)
        {
            if (AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode)
            {
                __result = GameInstance.GetPriceString(0f) + "/" + LocalizationManager.GetTranslation("day");
            }
        }
    }

    [HarmonyPatch(typeof(HireWorkerPanelUI), "Init")]
    private static class HireWorkerPanelUIPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled() || AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode;
        }

        private static void Postfix(HireWorkerPanelUI __instance)
        {
            ApplyExpectedPanelIcon(__instance);

            if (!AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode)
            {
                return;
            }

            HireWorkerPanelLevelRequiredField?.SetValue(__instance, 0);
            HireWorkerPanelTotalHireFeeField?.SetValue(__instance, 0f);
            __instance.m_SalaryCostText.text = GameInstance.GetPriceString(0f) + "/" + LocalizationManager.GetTranslation("day");
            __instance.m_HireFeeText.text = GameInstance.GetPriceString(0f);
            __instance.m_LevelRequirementText.gameObject.SetActive(value: false);
            __instance.m_HireFeeText.gameObject.SetActive(value: true);
            __instance.m_LockPurchaseBtn.gameObject.SetActive(value: false);
        }
    }

    [HarmonyPatch(typeof(HireWorkerPanelUI), "OnPressHireButton")]
    private static class HireWorkerPanelUIHirePatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled() || AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode;
        }

        private static void Prefix(HireWorkerPanelUI __instance)
        {
            int workerIndex = HireWorkerPanelIndexField?.GetValue(__instance) as int? ?? -1;
            Instance?.Logger.LogInfo($"Custom Workers detected OnPressHireButton for panel index {workerIndex}.");

            if (!AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode)
            {
                return;
            }

            HireWorkerPanelLevelRequiredField?.SetValue(__instance, 0);
            HireWorkerPanelTotalHireFeeField?.SetValue(__instance, 0f);
        }
    }

    [HarmonyPatch(typeof(ControllerButton), "OnPressConfirm")]
    private static class ControllerButtonConfirmPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled();
        }

        private static void Prefix(ControllerButton __instance)
        {
            HireWorkerPanelUI? panel = __instance.GetComponentInParent<HireWorkerPanelUI>();
            if (panel == null || !HireScreenCloneHelper.IsManagedClone(panel.GetComponentInParent<HireWorkerScreen>()))
            {
                return;
            }

            Button? button = __instance.m_Button;
            string buttonPath = button != null ? GetTransformPath(button.transform) : "<none>";
            string overlayInfo = __instance.m_OverlayButton != null ? __instance.m_OverlayButton.Count.ToString() : "0";
            Instance?.Logger.LogInfo($"Custom Workers controller confirm on ARC panel: panelPath={GetTransformPath(panel.transform)} buttonPath={buttonPath} buttonActive={button?.gameObject.activeInHierarchy} interactable={button?.interactable} overlayCount={overlayInfo}");
        }
    }

    [HarmonyPatch(typeof(Button), "Press")]
    private static class ButtonPressPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled();
        }

        private static void Prefix(Button __instance)
        {
            HireWorkerPanelUI? panel = __instance.GetComponentInParent<HireWorkerPanelUI>();
            if (panel != null && HireScreenCloneHelper.IsManagedClone(panel.GetComponentInParent<HireWorkerScreen>()))
            {
                Instance?.Logger.LogInfo($"Custom Workers live Button.Press on ARC panel button path={GetTransformPath(__instance.transform)} active={__instance.gameObject.activeInHierarchy} interactable={__instance.interactable}");
                return;
            }

            UI_PhoneScreen? phoneScreen = __instance.GetComponentInParent<UI_PhoneScreen>();
            if (phoneScreen != null)
            {
                Transform? current = __instance.transform;
                while (current != null)
                {
                    if (current.name.IndexOf("CustomWorkers_ArcRecruiter", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Instance?.Logger.LogInfo($"Custom Workers live Button.Press on ARC phone tile path={GetTransformPath(__instance.transform)} active={__instance.gameObject.activeInHierarchy} interactable={__instance.interactable}");
                        break;
                    }

                    current = current.parent;
                }
            }
        }
    }

    [HarmonyPatch(typeof(WorkerManager), "GetTotalSalaryCost")]
    private static class WorkerManagerSalaryPatch
    {
        private static bool Prepare()
        {
            return AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode;
        }

        private static void Postfix(ref float __result)
        {
            if (AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode)
            {
                __result = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(WorkerInteractUIScreen), "OpenScreen")]
    private static class WorkerInteractUIScreenPatch
    {
        private static bool Prepare()
        {
            return AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode;
        }

        private static void Postfix()
        {
            if (!AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode)
            {
                return;
            }

            CSingleton<WorkerInteractUIScreen>.Instance.m_SalaryCostText.text = GameInstance.GetPriceString(0f) + "/" + LocalizationManager.GetTranslation("day");
            CSingleton<WorkerInteractUIScreen>.Instance.m_BonusCostText.text = GameInstance.GetPriceString(0f);
            WorkerInteractSalaryCostField?.SetValue(CSingleton<WorkerInteractUIScreen>.Instance, 0f);
        }
    }

    [HarmonyPatch(typeof(RentBillScreen), "OnOpenScreen")]
    private static class RentBillScreenOpenPatch
    {
        private static bool Prepare()
        {
            return AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode;
        }

        private static void Prefix()
        {
            ZeroBillsIfMrBurnsMode();
        }
    }

    [HarmonyPatch(typeof(RentBillScreen), "EvaluateNewDayBill")]
    private static class RentBillScreenEvaluatePatch
    {
        private static bool Prepare()
        {
            return AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode;
        }

        private static void Postfix()
        {
            ZeroBillsIfMrBurnsMode();
        }
    }

    [HarmonyPatch(typeof(WorkerManager), "Start")]
    private static class WorkerManagerStartPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsRosterExtensionEnabled();
        }

        private static void Postfix(WorkerManager __instance)
        {
            if (!KillSwitchHelper.IsWorldReadyForRosterMutation())
            {
                Instance?.Logger.LogWarning($"Custom Workers WorkerManager.Start hard-blocked roster mutation because world is not ready. workerRuntimeCount={WorkerManager.GetWorkerList()?.Count ?? -1}");
                return;
            }

            Instance?.Logger.LogInfo($"Custom Workers WorkerManager.Start checkpoint: rosterCountBeforeExtend={__instance?.m_WorkerDataList?.Count ?? -1} workerRuntimeCount={WorkerManager.GetWorkerList()?.Count ?? -1}");
            ExtendWorkerRoster(__instance);
            Instance?.Logger.LogInfo($"Custom Workers WorkerManager.Start checkpoint: rosterCountAfterExtend={__instance?.m_WorkerDataList?.Count ?? -1} generatedCount={GeneratedWorkerAppearances.Count}");
        }
    }

    [HarmonyPatch(typeof(HireWorkerScreen), "Init")]
    private static class HireWorkerScreenInitPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled();
        }

        private static void Prefix(HireWorkerScreen __instance)
        {
            if (!IsArcUiEnabled() || !HireScreenCloneHelper.IsManagedClone(__instance))
            {
                WorkerManager? manager = CSingleton<WorkerManager>.Instance;
                if (manager?.m_WorkerDataList != null)
                {
                    int visibleWorkerCount = HireWorkerCloneRules.GetVisibleWorkerCount(isCloneScreen: false, manager.m_WorkerDataList.Count);
                    if (visibleWorkerCount < manager.m_WorkerDataList.Count)
                    {
                        originalWorkerDataListForVanillaHireScreen = manager.m_WorkerDataList;
                        manager.m_WorkerDataList = new List<WorkerData>(manager.m_WorkerDataList.GetRange(0, visibleWorkerCount));
                    }
                }

                return;
            }

            if (canonicalExpandedWorkerDataList != null && CSingleton<WorkerManager>.Instance != null)
            {
                CSingleton<WorkerManager>.Instance.m_WorkerDataList = canonicalExpandedWorkerDataList;
            }

            LogHelper.LogSliderLifecycle(Instance!.Logger, "hire-init-prefix", __instance, MaxPosXField, ScrollEndPosParentField, CanEvaluateMaxScrollPosField, MaxPosFoundField, MaxPosAccurateFoundField, PosXField, LerpPosXField);
            ExtendWorkerRoster(CSingleton<WorkerManager>.Instance);
            hirePanelsExpandedThisInit = HireHelper.EnsureHireWorkerPanels(__instance);
        }

        private static void Postfix(HireWorkerScreen __instance)
        {
            if (!IsArcUiEnabled() || !HireScreenCloneHelper.IsManagedClone(__instance))
            {
                if (originalWorkerDataListForVanillaHireScreen != null && CSingleton<WorkerManager>.Instance != null)
                {
                    CSingleton<WorkerManager>.Instance.m_WorkerDataList = originalWorkerDataListForVanillaHireScreen;
                    originalWorkerDataListForVanillaHireScreen = null;
                }

                return;
            }

            if (__instance?.m_HireWorkerPanelUIList == null || __instance.m_HireWorkerPanelUIList.Count == 0)
            {
                return;
            }

            LogHelper.LogSliderLifecycle(Instance!.Logger, "hire-init-postfix-before-refresh", __instance, MaxPosXField, ScrollEndPosParentField, CanEvaluateMaxScrollPosField, MaxPosFoundField, MaxPosAccurateFoundField, PosXField, LerpPosXField);

            if (hirePanelsExpandedThisInit)
            {
                ScrollHelper.RefreshExpandedHireWorkerLayout(__instance, ScrollEndPosParentField, EvaluateScrollerMethod);
            }

            EnsureCloneHirePanelsInitialized(__instance);

            LogHelper.LogHireWorkerDiagnostics(Instance!.Logger, __instance, MaxPosXField);
            LogHelper.LogSliderLifecycle(Instance!.Logger, "hire-init-postfix-after-refresh", __instance, MaxPosXField, ScrollEndPosParentField, CanEvaluateMaxScrollPosField, MaxPosFoundField, MaxPosAccurateFoundField, PosXField, LerpPosXField);

            string mode = hirePanelsExpandedThisInit ? "rebuilt" : "refreshed";
            Instance?.Logger.LogInfo($"Custom Workers {mode} hire worker screen for {__instance.m_HireWorkerPanelUIList.Count} panels.");
        }
    }

    [HarmonyPatch(typeof(HireWorkerScreen), "OnOpenScreen")]
    private static class HireWorkerScreenOpenPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled();
        }

        private static void Prefix(HireWorkerScreen __instance)
        {
            if (!IsArcUiEnabled() || !HireScreenCloneHelper.IsManagedClone(__instance))
            {
                return;
            }

            LogHelper.LogSliderLifecycle(Instance!.Logger, "hire-open-prefix", __instance, MaxPosXField, ScrollEndPosParentField, CanEvaluateMaxScrollPosField, MaxPosFoundField, MaxPosAccurateFoundField, PosXField, LerpPosXField);
        }

        private static void Postfix(HireWorkerScreen __instance)
        {
            if (!IsArcUiEnabled() || !HireScreenCloneHelper.IsManagedClone(__instance))
            {
                return;
            }

            LogHelper.LogSliderLifecycle(Instance!.Logger, "hire-open-postfix", __instance, MaxPosXField, ScrollEndPosParentField, CanEvaluateMaxScrollPosField, MaxPosFoundField, MaxPosAccurateFoundField, PosXField, LerpPosXField);
            if (HireWorkerLayoutRules.ShouldRefreshExpandedLayout(__instance.m_HireWorkerPanelUIList?.Count ?? 0))
            {
                Instance?.Logger.LogInfo("Custom Workers detected expanded hire screen during OnOpenScreen; refresh remains candidate for compatibility patching.");
            }
        }
    }

    [HarmonyPatch(typeof(UIScreenBase), "Start")]
    private static class ManagedCloneStartPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled();
        }

        private static void Postfix(UIScreenBase __instance)
        {
            HireWorkerScreen? hireScreen = __instance as HireWorkerScreen;
            if (!HireScreenCloneHelper.IsManagedClone(hireScreen) || hireScreen?.m_ScreenGroup == null)
            {
                return;
            }

            if (hireScreen.IsScreenOpened() && !hireScreen.m_ScreenGroup.activeSelf)
            {
                Instance?.Logger.LogWarning("Custom Workers detected UIScreenBase.Start() deactivating an already-open ARC Recruiter clone; restoring screen group immediately.");
                hireScreen.m_ScreenGroup.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(UIScreenBase), "OnCloseScreen")]
    private static class ManagedCloneClosePatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled();
        }

        private static void Postfix(UIScreenBase __instance)
        {
            HireWorkerScreen? hireScreen = __instance as HireWorkerScreen;
            if (!HireScreenCloneHelper.IsManagedClone(hireScreen))
            {
                return;
            }

            UI_PhoneScreen? phoneScreen = CSingleton<PhoneManager>.Instance?.m_UI_PhoneScreen;
            HireScreenCloneHelper.RestorePhoneStateAfterCloneClose(phoneScreen);
            PhoneOverhaulAppHelper.NotifyCloneClosed();
            Instance?.Logger.LogInfo("Custom Workers restored phone close state after ARC Recruiter clone close.");
        }
    }

    [HarmonyPatch(typeof(UIScreenBase), "OnPressBack")]
    private static class ManagedCloneBackPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled();
        }

        private static bool Prefix(UIScreenBase __instance)
        {
            HireWorkerScreen? hireScreen = __instance as HireWorkerScreen;
            if (!HireScreenCloneHelper.IsManagedClone(hireScreen))
            {
                return true;
            }

            HireScreenCloneHelper.OnArcCloseRequested(hireScreen);
            return false;
        }
    }

    [HarmonyPatch(typeof(UI_PhoneScreen), "OnPressHireWorkerBtn")]
    private static class PhoneHireButtonPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled();
        }

        private static bool Prefix()
        {
            return true;
        }
    }

    [HarmonyPatch(typeof(UI_PhoneScreen), "OnOpenScreen")]
    private static class PhoneOverhaulArcRecruiterPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled() && KillSwitchHelper.IsPhoneOverhaulHooksEnabled();
        }

        private static void Postfix(UI_PhoneScreen __instance)
        {
            Instance?.Logger.LogInfo("Custom Workers observed UI_PhoneScreen.OnOpenScreen postfix.");
            PhoneOverhaulAppHelper.EnsureRegistered(Instance!.Logger);
            PhoneOverhaulAppHelper.BeginTileBinding(__instance);
        }
    }

    [HarmonyPatch(typeof(UIScreenBase), "OpenScreen")]
    private static class PhoneOpenScreenFallbackPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsArcUiEnabled() && KillSwitchHelper.IsPhoneOverhaulHooksEnabled();
        }

        private static void Postfix(UIScreenBase __instance)
        {
            UI_PhoneScreen? phoneScreen = __instance as UI_PhoneScreen;
            if (phoneScreen == null)
            {
                return;
            }

            Instance?.Logger.LogInfo("Custom Workers observed UIScreenBase.OpenScreen postfix for UI_PhoneScreen.");
            PhoneOverhaulAppHelper.EnsureRegistered(Instance!.Logger);
            PhoneOverhaulAppHelper.BeginTileBinding(phoneScreen);
        }
    }

    [HarmonyPatch(typeof(WorkerData), "GetDescription")]
    private static class WorkerDataDescriptionPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsWorkerRuntimePatchEnabled() || KillSwitchHelper.IsRosterExtensionEnabled();
        }

        private static bool Prefix(WorkerData __instance, ref string __result)
        {
            string? description = __instance.description;
            if (!WorkerBioRules.IsRawText(description))
            {
                return true;
            }

            __result = WorkerBioRules.StripRawPrefix(description);
            return false;
        }
    }

    [HarmonyPatch(typeof(WorkerData), "GetBonusConversation")]
    private static class WorkerDataBonusConversationPatch
    {
        private static bool Prepare()
        {
            return KillSwitchHelper.IsWorkerRuntimePatchEnabled() || KillSwitchHelper.IsRosterExtensionEnabled();
        }

        private static bool Prefix(WorkerData __instance, ref string __result)
        {
            string? bonusConversation = __instance.bonusConversation;
            if (!WorkerBioRules.IsRawText(bonusConversation))
            {
                return true;
            }

            __result = WorkerBioRules.StripRawPrefix(bonusConversation);
            return false;
        }
    }

    private static void ExtendWorkerRoster(WorkerManager? manager)
    {
        if (KillSwitchHelper.TripIfDisabled(KillSwitchHelper.IsRosterExtensionEnabled(), "RosterExtension.ExtendWorkerRoster", Instance?.Logger))
        {
            return;
        }

        int runtimeWorkerCount = WorkerManager.GetWorkerList()?.Count ?? -1;
        if (!KillSwitchHelper.IsWorldReadyForRosterMutation())
        {
            Instance?.Logger.LogWarning($"Custom Workers hard-blocked ExtendWorkerRoster because world/save is not ready. runtimeWorkerCount={runtimeWorkerCount}");
            return;
        }

        if (workerRosterExtended)
        {
            Instance?.Logger.LogInfo("Custom Workers skipped roster extension: workerRosterExtended already true.");
            return;
        }

        if (manager == null)
        {
            Instance?.Logger.LogInfo("Custom Workers skipped roster extension: WorkerManager instance is null.");
            return;
        }

        if (manager.m_WorkerDataList == null)
        {
            Instance?.Logger.LogInfo("Custom Workers skipped roster extension: worker data list is null.");
            return;
        }

        if (manager.m_WorkerDataList.Count == 0)
        {
            Instance?.Logger.LogInfo("Custom Workers skipped roster extension: worker data list is empty.");
            return;
        }

        try
        {
            workerRosterExtended = true;
            Instance?.Logger.LogInfo($"Custom Workers ExtendWorkerRoster entered: inputRosterCount={manager.m_WorkerDataList.Count} runtimeWorkerCount={WorkerManager.GetWorkerList()?.Count ?? -1}");

            HashSet<string> usedNames = WorkerRosterRules.CollectUsedNames(manager.m_WorkerDataList);
            HashSet<string> usedFirstNames = WorkerRosterRules.CollectUsedFirstNames(manager.m_WorkerDataList);
            HashSet<string> usedLastNames = WorkerRosterRules.CollectUsedLastNames(manager.m_WorkerDataList);
            var generatedWorkerLogEntries = new List<GeneratedWorkerLogEntry>();

            int requestedWorkerSlots = System.Math.Max(1, AppearanceSettingsHelper.GetCurrentOptions().GeneratedWorkerCount);
            int maxGeneratedWorkerSlots = WorkerRosterRules.GetMaximumGeneratedWorkerCount(manager.m_WorkerDataList);
            int additionalWorkerSlots = WorkerRosterRules.GetGeneratedWorkerCount(manager.m_WorkerDataList);
            Instance?.Logger.LogInfo($"Custom Workers extending roster from {manager.m_WorkerDataList.Count} by {additionalWorkerSlots} generated workers (requested={requestedWorkerSlots}, max={maxGeneratedWorkerSlots}).");
            for (int slotOffset = 0; slotOffset < additionalWorkerSlots; slotOffset++)
            {
                int newIndex = manager.m_WorkerDataList.Count;
                WorkerData template = manager.m_WorkerDataList[manager.m_WorkerDataList.Count - 1];
                GeneratedWorkerAppearance appearance = WorkerHelper.BuildGeneratedWorkerAppearance(newIndex, slotOffset, MaxFemaleModelIndexField, MaxMaleModelIndexField);
                GeneratedWorkerAppearances[newIndex] = appearance;
                EnsureWorkerRestPoint(manager, newIndex);
                Worker? worker = EnsureRuntimeWorker(manager, newIndex);
                WorkerData generatedWorkerData = WorkerRosterRules.CreateGeneratedWorkerData(template, newIndex, slotOffset, appearance.IsFemale, usedNames, usedFirstNames, usedLastNames);
                manager.m_WorkerDataList.Add(generatedWorkerData);
                generatedWorkerLogEntries.Add(CreateGeneratedWorkerLogEntry(newIndex, slotOffset, generatedWorkerData, appearance));
                if (worker != null)
                {
                    worker.m_IsFemale = appearance.IsFemale;
                    WorkerHelper.ApplyGeneratedWorkerClothingColors(worker, ApparelObjectsField, GeneratedWorkerAppearances, AppearanceSettingsHelper.GetCurrentOptions());
                }
            }

            canonicalExpandedWorkerDataList = manager.m_WorkerDataList;
            LogHelper.WriteGeneratedWorkerJson(Instance!.Logger, generatedWorkerLogEntries);
            LogHelper.WriteWorkerRosterJson(Instance!.Logger, "extended", manager.m_WorkerDataList, GeneratedWorkerAppearances);
            Instance?.Logger.LogInfo($"Custom Workers extended worker roster to {manager.m_WorkerDataList.Count} entries.");
        }
        catch (System.Exception ex)
        {
            workerRosterExtended = false;
            Instance?.Logger.LogError($"Custom Workers failed to extend worker roster: {ex}");
        }
    }

    private static void EnsureWorkerRestPoint(WorkerManager manager, int newIndex)
    {
        if (manager.m_WorkerRestPointList == null || manager.m_WorkerRestPointList.Count == 0 || manager.m_WorkerRestPointList.Count > newIndex)
        {
            return;
        }

        Transform template = manager.m_WorkerRestPointList[manager.m_WorkerRestPointList.Count - 1];
        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.parent);
        clone.name = $"WorkerRestPoint{newIndex}";
        clone.transform.position = template.position + Vector3.right * 0.75f * (newIndex - manager.m_WorkerRestPointList.Count + 1);
        clone.transform.rotation = template.rotation;
        manager.m_WorkerRestPointList.Add(clone.transform);
    }

    private static Worker? EnsureRuntimeWorker(WorkerManager manager, int newIndex)
    {
        List<Worker> workers = WorkerManager.GetWorkerList();
        if (workers.Count > newIndex)
        {
            Worker existingWorker = workers[newIndex];
            if (existingWorker != null)
            {
                existingWorker.m_WorkerIndex = newIndex;
                existingWorker.InitializeCharacter();
                existingWorker.SetOutOfScreen();
            }

            return existingWorker;
        }

        if (SpawnedWorkerCountField != null)
        {
            SpawnedWorkerCountField.SetValue(manager, workers.Count);
        }

        AddWorkerPrefabMethod?.Invoke(manager, null);
        if (workers.Count <= newIndex)
        {
            return null;
        }

        Worker worker = workers[newIndex];
        if (worker == null)
        {
            return null;
        }

        worker.m_WorkerIndex = newIndex;
        worker.gameObject.name = $"Worker{newIndex}";
        worker.gameObject.SetActive(false);
        worker.InitializeCharacter();
        worker.SetOutOfScreen();
        return worker;
    }

    private static GeneratedWorkerLogEntry CreateGeneratedWorkerLogEntry(int workerIndex, int generatedSlotIndex, WorkerData workerData, GeneratedWorkerAppearance appearance)
    {
        Color32 hairColor = appearance.HairColor;
        return new GeneratedWorkerLogEntry(
            workerIndex,
            generatedSlotIndex,
            workerData.name,
            appearance.IsFemale,
            appearance.CharacterModelIndex,
            (appearance.CharacterScale.x, appearance.CharacterScale.y, appearance.CharacterScale.z),
            (hairColor.r, hairColor.g, hairColor.b, hairColor.a),
            workerData.costPerDay,
            workerData.hiringCost,
            workerData.restockSpeed,
            workerData.checkoutSpeed,
            workerData.walkSpeedMultiplier,
            WorkerBioRules.StripRawPrefix(workerData.description),
            WorkerBioRules.StripRawPrefix(workerData.bonusConversation));
    }

    private static void EnsureCloneHirePanelsInitialized(HireWorkerScreen screen)
    {
        WorkerManager? manager = CSingleton<WorkerManager>.Instance;
        if (manager?.m_WorkerDataList == null || screen?.m_HireWorkerPanelUIList == null)
        {
            return;
        }

        int visibleWorkerCount = HireWorkerCloneRules.GetVisibleWorkerCount(HireScreenCloneHelper.IsManagedClone(screen), manager.m_WorkerDataList.Count);
        Instance?.Logger.LogInfo($"Custom Workers clone panel init checkpoint: managedClone={HireScreenCloneHelper.IsManagedClone(screen)} visibleWorkerCount={visibleWorkerCount} rosterCount={manager.m_WorkerDataList.Count} generatedCount={GeneratedWorkerAppearances.Count}");
        for (int index = 0; index < screen.m_HireWorkerPanelUIList.Count; index++)
        {
            HireWorkerPanelUI panel = screen.m_HireWorkerPanelUIList[index];
            if (panel == null)
            {
                continue;
            }

            if (index < visibleWorkerCount)
            {
                panel.Init(screen, index);
                panel.SetActive(true);
            }
            else
            {
                panel.SetActive(false);
            }
        }
    }

    private static void ApplyExpectedPanelIcon(HireWorkerPanelUI panel)
    {
        if (panel?.m_IconImage == null)
        {
            return;
        }

        if (!(HireWorkerPanelIndexField?.GetValue(panel) is int workerIndex) || workerIndex < 0)
        {
            return;
        }

        WorkerData workerData = WorkerManager.GetWorkerData(workerIndex);
        if (workerData == null)
        {
            Instance?.Logger.LogWarning($"Custom Workers could not resolve WorkerData for panel index {workerIndex}.");
            return;
        }

        Sprite? expectedIcon = workerData.icon;
        string iconSource = "vanilla-worker-data";
        if (GeneratedWorkerAppearances.ContainsKey(workerIndex))
        {
            expectedIcon = WorkerIconRules.GetGeneratedWorkerIcon(workerData.icon);
            iconSource = "generated-worker-icon";
        }

        if (expectedIcon == null)
        {
            Instance?.Logger.LogWarning($"Custom Workers worker panel {workerIndex} expected icon from {iconSource} was null.");
            return;
        }

        if (panel.m_IconImage.sprite != expectedIcon)
        {
            panel.m_IconImage.sprite = expectedIcon;
            panel.m_IconImage.overrideSprite = null;
            panel.m_IconImage.enabled = true;
            panel.m_IconImage.gameObject.SetActive(true);
            Instance?.Logger.LogInfo($"Custom Workers applied {iconSource} to worker panel {workerIndex}.");
        }
    }

    private static void ZeroBillsIfMrBurnsMode()
    {
        if (!AppearanceSettingsHelper.GetCurrentOptions().MrBurnsMode)
        {
            return;
        }

        CPlayerData.SetBill(EBillType.Rent, 0, 0f);
        CPlayerData.SetBill(EBillType.Electric, 0, 0f);
        CPlayerData.SetBill(EBillType.Employee, 0, 0f);
    }

    private void LogCurrentMapPopulation()
    {
        List<Worker> workers = WorkerManager.GetWorkerList();
        int workerTotal = workers?.Count ?? 0;
        int workerActive = workers?.Count(worker => worker != null && worker.m_IsActive) ?? 0;

        int customerTotal = 0;
        int customerActive = 0;
        CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
        if (customerManager != null && CustomerListField?.GetValue(customerManager) is List<Customer> customers)
        {
            customerTotal = customers.Count;
            customerActive = customers.Count(customer => customer != null && customer.IsActive());
            LogHelper.WriteCustomerRosterJson(Logger, "map-population", customers);
        }

        Logger.LogInfo($"Custom Workers map population: activeWorkers={workerActive}/{workerTotal} activeCustomers={customerActive}/{customerTotal}");
    }

    private void WriteCustomerRosterEventSnapshot(string eventName, Customer customer)
    {
        CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
        if (customerManager == null || CustomerListField?.GetValue(customerManager) is not List<Customer> customers)
        {
            return;
        }

        int sequence = ++customerRosterEventSequence;
        int customerIndex = customers.IndexOf(customer);
        string snapshotName = $"{eventName}-{sequence:0000}-customer-{customerIndex}";
        LogHelper.WriteCustomerRosterJson(Logger, snapshotName, customers);
        Logger.LogInfo($"Custom Workers recorded customer roster snapshot for {eventName}: customerIndex={customerIndex} active={customer?.IsActive()} insideShop={customer?.IsInsideShop()}");
    }

}

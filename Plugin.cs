using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using CC;

namespace CustomWorkers;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
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
    private static readonly FieldInfo ScrollEndPosParentField = AccessTools.Field(typeof(GenericSliderScreen), "m_ScrollEndPosParent");
    private static readonly FieldInfo MaxPosXField = AccessTools.Field(typeof(GenericSliderScreen), "m_MaxPosX");
    private static readonly MethodInfo EvaluateScrollerMethod = AccessTools.Method(typeof(GenericSliderScreen), "EvaluateActiveRestockUIScroller");
    private static Plugin? Instance;
    private static bool workerRosterExtended;
    private static bool hirePanelsExpandedThisInit;
    private static readonly Dictionary<int, GeneratedWorkerAppearance> GeneratedWorkerAppearances = new Dictionary<int, GeneratedWorkerAppearance>();

    private void Awake()
    {
        Instance = this;
        HarmonyInstance.PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo("Custom Workers 0.2.0 loaded. NPC clothing colors + worker roster extension support.");
    }

    private void OnDestroy()
    {
        HarmonyInstance.UnpatchSelf();
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    [HarmonyPatch(typeof(Worker), "InitializeCharacter")]
    private static class WorkerInitializeCharacterPatch
    {
        private static bool Prefix(Worker __instance)
        {
            if (!WorkerHelper.TryGetGeneratedWorkerAppearance(__instance, GeneratedWorkerAppearances, out GeneratedWorkerAppearance? appearance))
            {
                return true;
            }

            __instance.m_IsFemale = appearance.IsFemale;
            if (!GeneratedWorkerAppearanceRules.ShouldApplyGeneratedCharacter(__instance.m_CharacterCustom != null))
            {
                return false;
            }

            __instance.m_CharacterCustom.CharacterName = GeneratedWorkerAppearanceRules.GetCharacterName(appearance.IsFemale, appearance.CharacterModelIndex);
            __instance.m_CharacterCustom.Initialize();
            IList? cardPackItemTypeList = WorkerCardPackItemTypeListField?.GetValue(__instance) as IList;
            IList? cardPackItemTypeEnabledList = WorkerCardPackItemTypeEnabledListField?.GetValue(__instance) as IList;
            if (cardPackItemTypeList?.Count == 0 && cardPackItemTypeEnabledList?.Count == 0)
            {
                for (int i = 0; i < CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_CardPackItemTypeList.Count; i++)
                {
                    cardPackItemTypeList?.Add(CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_CardPackItemTypeList[i]);
                    cardPackItemTypeEnabledList?.Add(true);
                }
            }

            if (__instance.m_TaskLevel.Count == 0 && __instance.m_ExpList.Count == 0)
            {
                for (int j = 0; j < 100; j++)
                {
                    __instance.m_TaskLevel.Add(0);
                    __instance.m_ExpList.Add(0);
                }
            }

            return false;
        }

        private static void Postfix(Worker __instance)
        {
            WorkerHelper.ApplyGeneratedWorkerClothingColors(__instance, ApparelObjectsField, GeneratedWorkerAppearances);
        }
    }

    [HarmonyPatch(typeof(WorkerManager), "Start")]
    private static class WorkerManagerStartPatch
    {
        private static void Postfix(WorkerManager __instance)
        {
            ExtendWorkerRoster(__instance);
        }
    }

    [HarmonyPatch(typeof(HireWorkerScreen), "Init")]
    private static class HireWorkerScreenInitPatch
    {
        private static void Prefix(HireWorkerScreen __instance)
        {
            ExtendWorkerRoster(CSingleton<WorkerManager>.Instance);
            hirePanelsExpandedThisInit = HireHelper.EnsureHireWorkerPanels(__instance);
        }

        private static void Postfix(HireWorkerScreen __instance)
        {
            if (__instance?.m_HireWorkerPanelUIList == null || __instance.m_HireWorkerPanelUIList.Count == 0)
            {
                return;
            }

            if (hirePanelsExpandedThisInit)
            {
                ScrollHelper.RefreshExpandedHireWorkerLayout(__instance, ScrollEndPosParentField, EvaluateScrollerMethod);
            }

            LogHelper.LogHireWorkerDiagnostics(Instance!.Logger, __instance, MaxPosXField);

            string mode = hirePanelsExpandedThisInit ? "rebuilt" : "refreshed";
            Instance?.Logger.LogInfo($"Custom Workers {mode} hire worker screen for {__instance.m_HireWorkerPanelUIList.Count} panels.");
        }
    }

    [HarmonyPatch(typeof(WorkerData), "GetDescription")]
    private static class WorkerDataDescriptionPatch
    {
        private static bool Prefix(WorkerData __instance, ref string __result)
        {
            if (!WorkerBioRules.IsRawText(__instance?.description))
            {
                return true;
            }

            __result = WorkerBioRules.StripRawPrefix(__instance.description);
            return false;
        }
    }

    [HarmonyPatch(typeof(WorkerData), "GetBonusConversation")]
    private static class WorkerDataBonusConversationPatch
    {
        private static bool Prefix(WorkerData __instance, ref string __result)
        {
            if (!WorkerBioRules.IsRawText(__instance?.bonusConversation))
            {
                return true;
            }

            __result = WorkerBioRules.StripRawPrefix(__instance.bonusConversation);
            return false;
        }
    }

    private static void ExtendWorkerRoster(WorkerManager? manager)
    {
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

            HashSet<string> usedNames = WorkerRosterRules.CollectUsedNames(manager.m_WorkerDataList);
            var generatedWorkerLogEntries = new List<GeneratedWorkerLogEntry>();

            int additionalWorkerSlots = WorkerRosterRules.GetGeneratedWorkerCount();
            Instance?.Logger.LogInfo($"Custom Workers extending roster from {manager.m_WorkerDataList.Count} by {additionalWorkerSlots} generated workers.");
            for (int slotOffset = 0; slotOffset < additionalWorkerSlots; slotOffset++)
            {
                int newIndex = manager.m_WorkerDataList.Count;
                WorkerData template = manager.m_WorkerDataList[manager.m_WorkerDataList.Count - 1];
                GeneratedWorkerAppearance appearance = WorkerHelper.BuildGeneratedWorkerAppearance(newIndex, slotOffset, MaxFemaleModelIndexField, MaxMaleModelIndexField);
                GeneratedWorkerAppearances[newIndex] = appearance;
                EnsureWorkerRestPoint(manager, newIndex);
                Worker? worker = EnsureRuntimeWorker(manager, newIndex);
                WorkerData generatedWorkerData = WorkerRosterRules.CreateGeneratedWorkerData(template, newIndex, slotOffset, appearance.IsFemale, usedNames);
                manager.m_WorkerDataList.Add(generatedWorkerData);
                generatedWorkerLogEntries.Add(CreateGeneratedWorkerLogEntry(newIndex, slotOffset, generatedWorkerData, appearance));
                if (worker != null)
                {
                    worker.m_IsFemale = appearance.IsFemale;
                    WorkerHelper.ApplyGeneratedWorkerClothingColors(worker, ApparelObjectsField, GeneratedWorkerAppearances);
                }
            }

            LogHelper.WriteGeneratedWorkerJson(Instance!.Logger, generatedWorkerLogEntries);
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
        GameObject clone = Object.Instantiate(template.gameObject, template.parent);
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

}

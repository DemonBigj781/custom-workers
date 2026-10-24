using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace CustomWorkers;

internal static class WorkerTaskBoardCompatibility
{
    private const string WorkerTaskBoardUiTypeName = "WorkerTaskBoard.WorkerTaskBoardUI";

    internal static void Install(Harmony harmony, ManualLogSource logger)
    {
        Type? uiType = AccessTools.TypeByName(WorkerTaskBoardUiTypeName);
        MethodInfo? gatherMethod = uiType == null ? null : AccessTools.Method(uiType, "GatherHiredWorkers");
        if (uiType == null || gatherMethod == null)
        {
            logger.LogInfo("Custom Workers did not find Worker Task Board; skipping optional worker-roster compatibility patch.");
            return;
        }

        FieldInfo? hiredField = AccessTools.Field(uiType, "_hired");
        MethodInfo? postfix = AccessTools.Method(typeof(WorkerTaskBoardCompatibility), nameof(GatherHiredWorkersPostfix));
        if (hiredField == null || postfix == null)
        {
            logger.LogWarning("Custom Workers found Worker Task Board but its private roster contract was not available; compatibility patch was skipped.");
            return;
        }

        harmony.Patch(gatherMethod, postfix: new HarmonyMethod(postfix));
        logger.LogInfo("Custom Workers installed Worker Task Board worker-roster compatibility patch.");
    }

    private static void GatherHiredWorkersPostfix(object __instance)
    {
        if (__instance == null)
        {
            return;
        }

        Type uiType = __instance.GetType();
        FieldInfo? hiredField = AccessTools.Field(uiType, "_hired");
        if (hiredField?.GetValue(__instance) is not List<Worker> hired)
        {
            return;
        }

        List<Worker>? workerList = WorkerManager.GetWorkerList();
        if (workerList == null || workerList.Count == 0)
        {
            return;
        }

        var candidates = new List<int>(workerList.Count);
        var hiredIndexes = new List<int>(workerList.Count);
        var existingIndexes = new List<int>(hired.Count);
        var workersByIndex = new Dictionary<int, Worker>();

        foreach (Worker? worker in workerList)
        {
            if (worker == null)
            {
                continue;
            }

            int workerIndex = worker.m_WorkerIndex;
            candidates.Add(workerIndex);
            workersByIndex[workerIndex] = worker;
            if (CPlayerData.GetIsWorkerHired(workerIndex))
            {
                hiredIndexes.Add(workerIndex);
            }
        }

        foreach (Worker? worker in hired)
        {
            if (worker != null)
            {
                existingIndexes.Add(worker.m_WorkerIndex);
            }
        }

        foreach (int workerIndex in WorkerTaskBoardRosterRules.GetMissingHiredWorkerIndexes(candidates, hiredIndexes, existingIndexes))
        {
            if (workersByIndex.TryGetValue(workerIndex, out Worker? worker))
            {
                hired.Add(worker);
            }
        }
    }
}

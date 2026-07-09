using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CustomWorkers;

internal static class PhoneOverhaulDebugHookHelper
{
    private static bool installed;
    private static BepInEx.Logging.ManualLogSource? logger;
    private static int patchedMethodCount;
    private static int runtimeHitCount;

    internal static bool HasSuccessfulInstall => installed && patchedMethodCount > 0;

    internal static bool HasRuntimeActivity => runtimeHitCount > 0;

    internal static string GetStatusSummary()
    {
        return $"installed={installed} patchedMethodCount={patchedMethodCount} runtimeHitCount={runtimeHitCount}";
    }

    internal static void Install(Harmony harmony, BepInEx.Logging.ManualLogSource logSource)
    {
        logger = logSource;
        if (installed)
        {
            logger?.LogInfo("Custom Workers Phone Overhaul debug hook install skipped: already installed.");
            return;
        }

        installed = true;
        patchedMethodCount = 0;
        logger?.LogInfo("Custom Workers starting Phone Overhaul debug hook installation.");
        TryPatch(harmony, "PhoneOverhaul.PhoneOverhaulPlugin", "Awake", nameof(OnPhoneOverhaulPluginAwakePrefix), nameof(OnPhoneOverhaulPluginAwakePostfix));
        TryPatch(harmony, "PhoneOverhaul.PhoneMasterHook", "RunPhoneOverhaulSequence", nameof(OnRunPhoneOverhaulSequencePrefix), nameof(OnRunPhoneOverhaulSequencePostfix));
        TryPatch(harmony, "PhoneOverhaul.PhoneMasterHook", "BuildAppTiles", nameof(OnBuildAppTilesPrefix), nameof(OnBuildAppTilesPostfix));
        TryPatch(harmony, "PhoneOverhaul.PhoneMasterHook", "PlaceApps", nameof(OnPlaceAppsPrefix), nameof(OnPlaceAppsPostfix));
        TryPatch(harmony, "PhoneOverhaul.PhoneOverhaulTextureReplacer", "ApplyAll", nameof(OnApplyAllPrefix), nameof(OnApplyAllPostfix));
        TryPatch(harmony, "PhoneOverhaul.PhoneOverhaulTextureReplacer", "TryGetStrictAppFolder", nameof(OnTryGetStrictAppFolderPrefix), nameof(OnTryGetStrictAppFolderPostfix));
        TryPatch(harmony, "PhoneOverhaul.PhoneOverhaulTextureReplacer", "TryLoadSprite", nameof(OnTryLoadSpritePrefix), nameof(OnTryLoadSpritePostfix));
        logger?.LogInfo("Custom Workers finished Phone Overhaul debug hook installation.");
    }

    private static void TryPatch(Harmony harmony, string typeFullName, string methodName, string prefixName, string postfixName)
    {
        MethodInfo? target = FindMethod(typeFullName, methodName);
        if (target == null)
        {
            logger?.LogWarning($"Custom Workers could not find Phone Overhaul method {typeFullName}.{methodName} for debug patching.");
            return;
        }

        HarmonyMethod? prefix = GetPatchMethod(prefixName);
        HarmonyMethod? postfix = GetPatchMethod(postfixName);
        try
        {
            harmony.Patch(target, prefix, postfix);
            patchedMethodCount++;
            logger?.LogInfo($"Custom Workers installed Phone Overhaul debug patch on {typeFullName}.{methodName}.");
        }
        catch (Exception ex)
        {
            logger?.LogError($"Custom Workers failed to patch Phone Overhaul method {typeFullName}.{methodName}: {ex}");
        }
    }

    private static HarmonyMethod? GetPatchMethod(string methodName)
    {
        MethodInfo? method = typeof(PhoneOverhaulDebugHookHelper).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        return method != null ? new HarmonyMethod(method) : null;
    }

    private static MethodInfo? FindMethod(string typeFullName, string methodName)
    {
        Type? type = FindType(typeFullName);
        return type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
    }

    private static Type? FindType(string typeFullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? type = assembly.GetType(typeFullName, throwOnError: false);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static void OnPhoneOverhaulPluginAwakePrefix()
    {
        runtimeHitCount++;
        LogHelper.LogRuntimeDebug("Custom Workers observed Phone Overhaul plugin Awake prefix.");
    }

    private static void OnPhoneOverhaulPluginAwakePostfix()
    {
        runtimeHitCount++;
        LogHelper.LogRuntimeDebug("Custom Workers observed Phone Overhaul plugin Awake postfix.");
    }

    private static void OnRunPhoneOverhaulSequencePrefix()
    {
        runtimeHitCount++;
        LogHelper.LogRuntimeDebug("Custom Workers observed Phone Overhaul RunPhoneOverhaulSequence prefix.");
    }

    private static void OnRunPhoneOverhaulSequencePostfix()
    {
        runtimeHitCount++;
        LogHelper.LogRuntimeDebug("Custom Workers observed Phone Overhaul RunPhoneOverhaulSequence postfix.");
    }

    private static void OnBuildAppTilesPrefix()
    {
        runtimeHitCount++;
        LogHelper.LogRuntimeDebug("Custom Workers observed Phone Overhaul BuildAppTiles prefix.");
    }

    private static void OnBuildAppTilesPostfix()
    {
        runtimeHitCount++;
        LogHelper.LogRuntimeDebug("Custom Workers observed Phone Overhaul BuildAppTiles postfix.");
    }

    private static void OnPlaceAppsPrefix()
    {
        runtimeHitCount++;
        LogHelper.LogRuntimeDebug("Custom Workers observed Phone Overhaul PlaceApps prefix.");
    }

    private static void OnPlaceAppsPostfix()
    {
        runtimeHitCount++;
        LogHelper.LogRuntimeDebug("Custom Workers observed Phone Overhaul PlaceApps postfix.");
    }

    private static void OnApplyAllPrefix(object[] __args)
    {
        runtimeHitCount++;
        if (__args.Length >= 5)
        {
            string appId = __args[1]?.ToString() ?? "<null>";
            string iconKey = __args[2]?.ToString() ?? "<null>";
            string innerKey = __args[3]?.ToString() ?? "<null>";
            string outerKey = __args[4]?.ToString() ?? "<null>";
            LogHelper.LogRuntimeDebug($"Custom Workers observed Phone Overhaul starting icon load in ApplyAll: appId={appId} iconKey={iconKey} innerKey={innerKey} outerKey={outerKey}.");
        }
    }

    private static void OnApplyAllPostfix(object[] __args)
    {
        runtimeHitCount++;
        if (__args.Length >= 5)
        {
            string appId = __args[1]?.ToString() ?? "<null>";
            LogHelper.LogRuntimeDebug($"Custom Workers observed Phone Overhaul ApplyAll postfix: appId={appId}.");
        }
    }

    private static void OnTryGetStrictAppFolderPrefix(string appId)
    {
        runtimeHitCount++;
        if (IsArcRecruiter(appId))
        {
            LogHelper.LogRuntimeDebug($"Custom Workers observed Phone Overhaul TryGetStrictAppFolder prefix for appId={appId}.");
        }
    }

    private static void OnTryGetStrictAppFolderPostfix(string appId, string appDir, bool __result)
    {
        runtimeHitCount++;
        if (IsArcRecruiter(appId))
        {
            LogHelper.LogRuntimeDebug($"Custom Workers observed Phone Overhaul TryGetStrictAppFolder postfix for appId={appId}: result={__result} appDir={appDir ?? "<null>"}.");
        }
    }

    private static void OnTryLoadSpritePrefix(string appDir, string nameOrWithExt)
    {
        runtimeHitCount++;
        if (LooksLikeArcRecruiterPath(appDir))
        {
            LogHelper.LogRuntimeDebug($"Custom Workers observed Phone Overhaul TryLoadSprite prefix: appDir={appDir ?? "<null>"} name={nameOrWithExt ?? "<null>"}.");
        }
    }

    private static void OnTryLoadSpritePostfix(string appDir, string nameOrWithExt, string resolvedPath, bool __result)
    {
        runtimeHitCount++;
        if (LooksLikeArcRecruiterPath(appDir))
        {
            LogHelper.LogRuntimeDebug($"Custom Workers observed Phone Overhaul TryLoadSprite postfix: appDir={appDir ?? "<null>"} name={nameOrWithExt ?? "<null>"} result={__result} resolvedPath={resolvedPath ?? "<null>"}.");
        }
    }

    private static bool IsArcRecruiter(string? appId)
    {
        if (string.IsNullOrEmpty(appId))
        {
            return false;
        }

        string nonNullAppId = appId!;
        return nonNullAppId.IndexOf("CustomWorkers_ArcRecruiter", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool LooksLikeArcRecruiterPath(string? appDir)
    {
        if (string.IsNullOrEmpty(appDir))
        {
            return false;
        }

        string nonNullAppDir = appDir!;
        return nonNullAppDir.IndexOf("CustomWorkers_ArcRecruiter", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

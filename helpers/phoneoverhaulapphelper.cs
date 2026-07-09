using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CustomWorkers;

internal static class PhoneOverhaulAppHelper
{
    private const string AppId = "CustomWorkers_ArcRecruiter";
    private const string DisplayName = "ARC Recruiter";

    private static bool registered;
    private static ManualLogSource? log;
    private static readonly HashSet<int> fallbackDumpedScreenIds = new HashSet<int>();
    private static readonly Dictionary<int, int> registrationAttemptsByScreen = new Dictionary<int, int>();
    private static bool appOpenInProgress;

    private sealed class ArcRecruiterTileBinder : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                UI_PhoneScreen? phoneScreen = GetComponent<UI_PhoneScreen>();
                if (phoneScreen != null)
                {
                    PhoneOverhaulAppHelper.ApplyTileVisuals(phoneScreen);
                }

                yield return null;
            }
        }
    }

    internal static void EnsureRegistered(ManualLogSource logger)
    {
        log = logger;
        if (KillSwitchHelper.TripIfDisabled(KillSwitchHelper.IsArcUiEnabled() && KillSwitchHelper.IsPhoneOverhaulHooksEnabled(), "PhoneOverhaul.EnsureRegistered", logger))
        {
            return;
        }

        log?.LogInfo($"Custom Workers ARC Recruiter registration check: registered={registered}.");
        if (registered)
        {
            log?.LogInfo("Custom Workers ARC Recruiter app registration skipped: already registered.");
            return;
        }

        Type? apiType = Type.GetType("PhoneOverhaul.API.PhoneOverhaulAPI, Phone - Overhaul");
        if (apiType == null)
        {
            log?.LogWarning("Custom Workers could not find Phone Overhaul API type during registration.");
            return;
        }

        object? registry = apiType.GetProperty("Registry", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (registry == null)
        {
            log?.LogWarning("Custom Workers found Phone Overhaul but Registry was null during registration.");
            return;
        }

        Type? appSpecType = Type.GetType("PhoneOverhaul.API.AppSpec, Phone - Overhaul");
        if (appSpecType == null)
        {
            log?.LogWarning("Custom Workers could not resolve Phone Overhaul AppSpec type.");
            return;
        }

        object appSpec = Activator.CreateInstance(appSpecType)!;
        appSpecType.GetField("AppId")?.SetValue(appSpec, AppId);
        appSpecType.GetField("DisplayName")?.SetValue(appSpec, DisplayName);
        appSpecType.GetField("OnClick")?.SetValue(appSpec, (Action)OnAppClicked);
        log?.LogInfo("Custom Workers attempting Phone Overhaul registration for ARC Recruiter.");

        object? handle = registry.GetType().GetMethod("Register")?.Invoke(registry, new[] { appSpec });
        if (handle == null)
        {
            log?.LogWarning("Custom Workers ARC Recruiter app registration returned a null handle.");
            return;
        }

        PropertyInfo? isValidProperty = handle.GetType().GetProperty("IsValid", BindingFlags.Public | BindingFlags.Instance);
        bool isValid = isValidProperty?.GetValue(handle) as bool? ?? false;
        if (!isValid)
        {
            log?.LogWarning("Custom Workers ARC Recruiter app registration returned an invalid handle.");
            return;
        }

        registered = true;
        log?.LogInfo("Custom Workers successfully registered ARC Recruiter with Phone Overhaul.");
    }

    internal static void ApplyTileVisuals(UI_PhoneScreen phoneScreen)
    {
        try
        {
            if (phoneScreen == null)
            {
                return;
            }

            if (!registered)
            {
                return;
            }

            LogPhoneScreenState("ApplyTileVisuals-start", phoneScreen);

            int screenId = phoneScreen.GetInstanceID();
            int attempt = IncrementRegistrationAttempt(screenId);
            List<Transform> matchingTiles = FindArcRecruiterTiles(phoneScreen.transform);
            bool tileAlreadyExisted = matchingTiles.Count > 0;
            log?.LogInfo($"Custom Workers ARC Recruiter tile scan: screenId={screenId} attempt={attempt} path={GetTransformPath(phoneScreen.transform)} activeInHierarchy={phoneScreen.gameObject.activeInHierarchy} matchesBefore={matchingTiles.Count} hookStatus={PhoneOverhaulDebugHookHelper.GetStatusSummary()}.");
            if (matchingTiles.Count == 0)
            {
                log?.LogInfo("Custom Workers did not find the ARC Recruiter tile yet during this phone refresh pass.");
                DumpFallbackPhoneScreenTree(phoneScreen, "arc-recruiter-tile-missing");
                return;
            }

            if (matchingTiles.Count > 1)
            {
                for (int duplicateIndex = 1; duplicateIndex < matchingTiles.Count; duplicateIndex++)
                {
                    Transform duplicateTile = matchingTiles[duplicateIndex];
                    log?.LogWarning($"Custom Workers detected duplicate ARC Recruiter tile '{duplicateTile.name}' at {GetTransformPath(duplicateTile)}; deactivating duplicate.");
                    duplicateTile.gameObject.SetActive(false);
                }
            }

            Transform tile = matchingTiles[0];

            log?.LogInfo($"Custom Workers found ARC Recruiter tile '{tile.name}' and is applying custom visuals. tilePath={GetTransformPath(tile)} tileAlreadyExisted={tileAlreadyExisted} activeInHierarchy={tile.gameObject.activeInHierarchy}");

            int clickListenerCountBefore = CountPersistentButtonListeners(tile);
            bool assignedIcon = ArcRecruiterAssetLayerHelper.ApplyTileIcon(tile, log);

            TMP_Text[] labels = tile.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                TMP_Text label = labels[index];
                if (label == null)
                {
                    continue;
                }

                string name = label.gameObject.name.ToLowerInvariant();
                if (name.Contains("text") || name.Contains("label"))
                {
                    label.text = DisplayName;
                }
            }

            int clickListenerCountAfter = CountPersistentButtonListeners(tile);
            log?.LogInfo($"Custom Workers ARC Recruiter tile apply complete: screenId={screenId} assignedIcon={assignedIcon} labelCount={labels.Length} matchesAfter={FindArcRecruiterTiles(phoneScreen.transform).Count} clickListenersBefore={clickListenerCountBefore} clickListenersAfter={clickListenerCountAfter}.");
            LogPhoneScreenState("ApplyTileVisuals-end", phoneScreen);
        }
        catch (Exception ex)
        {
            log?.LogError($"Custom Workers failed while applying ARC Recruiter tile visuals: {ex}");
        }
    }

    private static void OnAppClicked()
    {
        try
        {
            if (appOpenInProgress)
            {
                log?.LogWarning("Custom Workers ARC Recruiter OnClick ignored because an open is already in progress.");
                return;
            }

            appOpenInProgress = true;
            log?.LogInfo("Custom Workers ARC Recruiter OnClick invoked.");
            UI_PhoneScreen? phoneScreen = CSingleton<PhoneManager>.Instance?.m_UI_PhoneScreen;
            if (phoneScreen == null)
            {
                log?.LogWarning("Custom Workers ARC Recruiter OnClick could not find UI_PhoneScreen.");
                appOpenInProgress = false;
                return;
            }

            LogPhoneScreenState("OnAppClicked-before-open", phoneScreen);

            bool opened = HireScreenCloneHelper.OpenClone(phoneScreen);
            log?.LogInfo($"Custom Workers ARC Recruiter OnClick open result={opened}.");
            if (!opened)
            {
                appOpenInProgress = false;
            }
            LogPhoneScreenState("OnAppClicked-after-open", phoneScreen);
        }
        catch (Exception ex)
        {
            appOpenInProgress = false;
            log?.LogError($"Custom Workers ARC Recruiter OnClick failed: {ex}");
        }
    }

    internal static void NotifyCloneStabilized()
    {
        appOpenInProgress = false;
    }

    internal static void NotifyCloneClosed()
    {
        appOpenInProgress = false;
    }

    private static List<Transform> FindArcRecruiterTiles(Transform root)
    {
        string expectedSuffix = AppId;
        var matches = new List<Transform>();
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform transform = transforms[index];
            if (transform.name != null && transform.name.IndexOf(expectedSuffix, StringComparison.Ordinal) >= 0)
            {
                matches.Add(transform);
            }
        }

        return matches;
    }

    private static void LogFallbackSpriteState(Image image, string phase)
    {
        if (image == null)
        {
            return;
        }

        string spriteName = image.sprite != null ? image.sprite.name : "<none>";
        string overrideSpriteName = image.overrideSprite != null ? image.overrideSprite.name : "<none>";
        bool looksLikeFallback = IsFallbackSpriteName(spriteName) || IsFallbackSpriteName(overrideSpriteName);
        log?.LogInfo($"Custom Workers ARC Recruiter tile image {phase}: object={image.gameObject.name} sprite={spriteName} overrideSprite={overrideSpriteName} fallback={looksLikeFallback} activeSelf={image.gameObject.activeSelf} activeInHierarchy={image.gameObject.activeInHierarchy}");
    }

    private static bool IsFallbackSpriteName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName) || spriteName == "<none>")
        {
            return false;
        }

        string lowered = spriteName.ToLowerInvariant();
        return lowered.Contains("missing")
            || lowered.Contains("fallback")
            || lowered.Contains("error")
            || lowered.Contains("question")
            || lowered.Contains("unknown");
    }

    internal static void BeginTileBinding(UI_PhoneScreen phoneScreen)
    {
        try
        {
            if (KillSwitchHelper.TripIfDisabled(KillSwitchHelper.IsArcUiEnabled() && KillSwitchHelper.IsPhoneOverhaulHooksEnabled(), "PhoneOverhaul.BeginTileBinding", log))
            {
                return;
            }

            if (phoneScreen == null)
            {
                return;
            }

            log?.LogInfo("Custom Workers BeginTileBinding invoked for UI_PhoneScreen.");
            log?.LogInfo($"Custom Workers BeginTileBinding screenState: id={phoneScreen.GetInstanceID()} path={GetTransformPath(phoneScreen.transform)} activeInHierarchy={phoneScreen.gameObject.activeInHierarchy}");
            log?.LogInfo($"Custom Workers BeginTileBinding Phone Overhaul hook status: {PhoneOverhaulDebugHookHelper.GetStatusSummary()}");

            if (phoneScreen.GetComponent<ArcRecruiterTileBinder>() == null)
            {
                phoneScreen.gameObject.AddComponent<ArcRecruiterTileBinder>();
                log?.LogInfo("Custom Workers attached ARC Recruiter tile binder to UI_PhoneScreen.");
            }

            HireScreenCloneHelper.PrewarmCloneIfPossible(phoneScreen);

            if (!PhoneOverhaulDebugHookHelper.HasSuccessfulInstall || !PhoneOverhaulDebugHookHelper.HasRuntimeActivity)
            {
                DumpFallbackPhoneScreenTree(phoneScreen, "phone-overhaul-hook-missing");
            }

            ApplyTileVisuals(phoneScreen);
        }
        catch (Exception ex)
        {
            log?.LogError($"Custom Workers BeginTileBinding failed: {ex}");
        }
    }

    private static void DumpFallbackPhoneScreenTree(UI_PhoneScreen phoneScreen, string reason)
    {
        int screenId = phoneScreen.GetInstanceID();
        if (fallbackDumpedScreenIds.Contains(screenId))
        {
            return;
        }

        fallbackDumpedScreenIds.Add(screenId);
        log?.LogWarning($"Custom Workers fallback phone screen discovery engaged: reason={reason} screenId={screenId}.");

        try
        {
            BepInEx.Logging.ManualLogSource? activeLogger = log;
            if (activeLogger != null)
            {
                LogHelper.WriteUiAssetInventoryHtml(activeLogger, $"phone-screen-{reason}-{screenId}", phoneScreen.transform);
            }
        }
        catch (Exception ex)
        {
            log?.LogError($"Custom Workers fallback phone screen discovery failed: {ex}");
        }
    }

    private static void LogPhoneScreenState(string phase, UI_PhoneScreen phoneScreen)
    {
        int tileCount = phoneScreen.GetComponentsInChildren<Button>(true).Length;
        log?.LogInfo($"Custom Workers phone screen {phase}: id={phoneScreen.GetInstanceID()} path={GetTransformPath(phoneScreen.transform)} activeInHierarchy={phoneScreen.gameObject.activeInHierarchy} buttonCount={tileCount}");
    }

    private static int IncrementRegistrationAttempt(int screenId)
    {
        int nextAttempt = registrationAttemptsByScreen.TryGetValue(screenId, out int currentAttempt)
            ? currentAttempt + 1
            : 1;

        registrationAttemptsByScreen[screenId] = nextAttempt;
        return nextAttempt;
    }

    private static int CountPersistentButtonListeners(Transform tile)
    {
        int listenerCount = 0;
        Button[] buttons = tile.GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            int persistentCount = button.onClick != null ? button.onClick.GetPersistentEventCount() : -1;
            listenerCount += persistentCount > 0 ? persistentCount : 0;
            log?.LogInfo($"Custom Workers ARC Recruiter tile button state: object={button.gameObject.name} persistentListeners={persistentCount} interactable={button.interactable} activeInHierarchy={button.gameObject.activeInHierarchy}");
        }

        return listenerCount;
    }

    private static string GetTransformPath(Transform transform)
    {
        var segments = new System.Collections.Generic.Stack<string>();
        Transform? current = transform;
        while (current != null)
        {
            segments.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", segments.ToArray());
    }
}

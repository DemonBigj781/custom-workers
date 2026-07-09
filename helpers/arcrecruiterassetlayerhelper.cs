using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace CustomWorkers;

internal static class ArcRecruiterAssetLayerHelper
{
    private enum AssetLoadState
    {
        NotLoaded,
        Loaded,
        Failed,
    }

    private static Sprite? cachedAppIcon;
    private static Sprite? cachedHeaderLogo;
    private static Sprite? cachedWorkerIcon;
    private static AssetLoadState appIconState;
    private static AssetLoadState headerLogoState;
    private static AssetLoadState workerIconState;

    internal static void PreloadCoreAssets(ManualLogSource? logger)
    {
        if (KillSwitchHelper.TripIfDisabled(KillSwitchHelper.IsArcAssetLoaderEnabled(), "ArcAssetLoader.PreloadCoreAssets", logger))
        {
            return;
        }

        logger?.LogInfo("Custom Workers ARC asset preload starting.");
        _ = GetAppIconSprite(logger);
        _ = GetHeaderLogoSprite(logger);
        _ = GetWorkerIconSprite(logger);
        logger?.LogInfo($"Custom Workers ARC asset preload finished: {GetAssetLoadSummary()}.");
    }

    internal static Sprite? GetAppIconSprite()
    {
        return GetAppIconSprite(null);
    }

    internal static Sprite? GetAppIconSprite(ManualLogSource? logger)
    {
        return GetOrCreateSprite(ref cachedAppIcon, ref appIconState, ArcRecruiterAppIconBase64Data.Value, "arc-recruiter-app-icon-embedded", "CustomWorkers_ArcRecruiterAppIcon", logger);
    }

    internal static Sprite? GetHeaderLogoSprite()
    {
        return GetHeaderLogoSprite(null);
    }

    internal static Sprite? GetHeaderLogoSprite(ManualLogSource? logger)
    {
        return GetOrCreateSprite(ref cachedHeaderLogo, ref headerLogoState, ArcRecruiterHeaderLogoBase64Data.Value, "arc-recruiter-header-logo-embedded", "CustomWorkers_ArcRecruiterHeaderLogo", logger);
    }

    internal static Sprite? GetWorkerIconSprite()
    {
        return GetWorkerIconSprite(null);
    }

    internal static Sprite? GetWorkerIconSprite(ManualLogSource? logger)
    {
        return GetOrCreateSprite(ref cachedWorkerIcon, ref workerIconState, WorkerIconBase64Data.Value, "generated-worker-icon-embedded", "CustomWorkers_GeneratedWorkerIcon", logger);
    }

    internal static string GetAssetLoadSummary()
    {
        return $"appIcon={DescribeState(appIconState)},headerLogo={DescribeState(headerLogoState)},workerIcon={DescribeState(workerIconState)}";
    }

    internal static void ApplyOwnedAssets(HireWorkerScreen? screen, IReadOnlyDictionary<int, GeneratedWorkerAppearance> generatedAppearances, ManualLogSource? logger, bool liveTemplateReady)
    {
        if (KillSwitchHelper.TripIfDisabled(KillSwitchHelper.IsArcAssetLoaderEnabled(), "ArcAssetLoader.ApplyOwnedAssets", logger))
        {
            return;
        }

        if (screen?.m_ScreenGroup == null)
        {
            return;
        }

        logger?.LogInfo($"Custom Workers ARC asset layer applying to screen={screen.name} liveTemplateReady={liveTemplateReady} source={GetAssetLoadSummary()}.");
        ApplyBackgroundTheme(screen.m_ScreenGroup.transform, logger);
        ApplyHeaderLogo(screen, logger);
        ApplyWorkerIcons(screen, generatedAppearances, logger);
        EnsureFallbackSkeleton(screen, logger, liveTemplateReady);
    }

    internal static bool ApplyTileIcon(Transform tile, ManualLogSource? logger)
    {
        if (KillSwitchHelper.TripIfDisabled(KillSwitchHelper.IsArcAssetLoaderEnabled(), "ArcAssetLoader.ApplyTileIcon", logger))
        {
            return false;
        }

        Sprite? icon = GetAppIconSprite(logger);
        if (icon == null)
        {
            logger?.LogWarning("Custom Workers ARC asset layer could not provide an app icon sprite for tile injection.");
            return false;
        }

        bool applied = false;
        Image[] images = tile.GetComponentsInChildren<Image>(true);
        for (int index = 0; index < images.Length; index++)
        {
            Image image = images[index];
            string name = image.gameObject.name;
            string lowerName = name.ToLowerInvariant();
            if (!string.Equals(name, "Icon", StringComparison.Ordinal)
                && !string.Equals(name, "Sprite", StringComparison.Ordinal)
                && !string.Equals(name, "Icon2", StringComparison.Ordinal)
                && !lowerName.Contains("icon")
                && !lowerName.Contains("sprite"))
            {
                continue;
            }

            image.sprite = icon;
            image.overrideSprite = null;
            image.color = Color.white;
            image.material = null;
            image.raycastTarget = false;
            image.enabled = true;
            image.gameObject.SetActive(true);
            applied = true;
            logger?.LogInfo($"Custom Workers ARC asset layer assigned app icon to tile image path={GetTransformPath(image.transform)}.");
        }

        return applied;
    }

    private static void ApplyBackgroundTheme(Transform root, ManualLogSource? logger)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        int recolored = 0;
        for (int index = 0; index < images.Length; index++)
        {
            Image image = images[index];
            if (image == null)
            {
                continue;
            }

            if (image.sprite != null && string.Equals(image.sprite.name, "panel-inside_white", StringComparison.Ordinal) && IsArcBlue(image.color))
            {
                image.color = new Color(0f, 0f, 0f, image.color.a);
                recolored++;
                continue;
            }

            string lowerName = image.gameObject.name.ToLowerInvariant();
            if ((lowerName.Contains("header") || lowerName.Contains("footer") || lowerName.Contains("topheader") || lowerName.Contains("bottomfooter") || lowerName.Contains("bg")) && IsArcBlue(image.color))
            {
                image.color = new Color(0f, 0f, 0f, image.color.a);
                recolored++;
            }
        }

        logger?.LogInfo($"Custom Workers ARC asset layer recolored {recolored} blue UI images to black.");
    }

    private static void ApplyHeaderLogo(HireWorkerScreen screen, ManualLogSource? logger)
    {
        Sprite? logo = GetHeaderLogoSprite(logger);
        if (logo == null || screen.m_ScreenGroup == null)
        {
            return;
        }

        Transform? existing = screen.m_ScreenGroup.transform.Find("CustomWorkers_ArcRecruiterHeaderLogo");
        GameObject logoObject;
        if (existing == null)
        {
            logoObject = new GameObject("CustomWorkers_ArcRecruiterHeaderLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logoObject.transform.SetParent(screen.m_ScreenGroup.transform, false);
        }
        else
        {
            logoObject = existing.gameObject;
        }

        RectTransform rect = (RectTransform)logoObject.transform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -18f);
        rect.sizeDelta = new Vector2(420f, 122f);

        Image image = logoObject.GetComponent<Image>();
        image.sprite = logo;
        image.overrideSprite = null;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = true;
        logoObject.SetActive(true);
        logoObject.transform.SetAsLastSibling();
        logger?.LogInfo($"Custom Workers ARC asset layer assigned header logo at {GetTransformPath(logoObject.transform)}.");
    }

    private static void ApplyWorkerIcons(HireWorkerScreen screen, IReadOnlyDictionary<int, GeneratedWorkerAppearance> generatedAppearances, ManualLogSource? logger)
    {
        Sprite? workerIcon = GetWorkerIconSprite(logger);
        if (workerIcon == null || screen.m_HireWorkerPanelUIList == null)
        {
            return;
        }

        int assigned = 0;
        for (int index = 0; index < screen.m_HireWorkerPanelUIList.Count; index++)
        {
            HireWorkerPanelUI? panel = screen.m_HireWorkerPanelUIList[index];
            if (panel?.m_IconImage == null)
            {
                continue;
            }

            if (!generatedAppearances.ContainsKey(index))
            {
                continue;
            }

            panel.m_IconImage.sprite = workerIcon;
            panel.m_IconImage.overrideSprite = null;
            panel.m_IconImage.color = Color.white;
            panel.m_IconImage.enabled = true;
            assigned++;
        }

        logger?.LogInfo($"Custom Workers ARC asset layer assigned generated worker icons to {assigned} panels.");
    }

    private static void EnsureFallbackSkeleton(HireWorkerScreen screen, ManualLogSource? logger, bool liveTemplateReady)
    {
        if (liveTemplateReady)
        {
            return;
        }

        if (screen.m_HireWorkerPanelUIList != null && screen.m_HireWorkerPanelUIList.Count > 0)
        {
            logger?.LogInfo("Custom Workers ARC asset layer skipped fallback skeleton because the cloned HireWorkerScreen already has structural panel data.");
            return;
        }

        if (screen.m_ScreenGroup != null)
        {
            screen.m_ScreenGroup.SetActive(true);
        }

        logger?.LogWarning("Custom Workers ARC asset layer entered non-destructive fallback mode because Go Hire live structure was not ready. No purchase, lock, or hired state was forcibly changed.");
    }

    private static Sprite? GetOrCreateSprite(ref Sprite? cache, ref AssetLoadState state, string? base64, string sourceName, string spriteName, ManualLogSource? logger)
    {
        if (cache != null)
        {
            state = AssetLoadState.Loaded;
            return cache;
        }

        if (state == AssetLoadState.Failed)
        {
            logger?.LogWarning($"Custom Workers ARC asset layer skipped {spriteName}: previous load attempt already failed.");
            return null;
        }

        logger?.LogInfo($"Custom Workers ARC asset layer loading sprite {spriteName} from {sourceName}.");

        if (!Base64SpriteHelper.TryDecodePng(base64, out byte[]? pngBytes, sourceName) || pngBytes == null)
        {
            state = AssetLoadState.Failed;
            logger?.LogWarning($"Custom Workers ARC asset layer failed to decode sprite {spriteName} from {sourceName}.");
            return null;
        }

        try
        {
            cache = Base64SpriteHelper.CreateSpriteFromPng(pngBytes, spriteName);
            state = cache != null ? AssetLoadState.Loaded : AssetLoadState.Failed;
            logger?.LogInfo($"Custom Workers ARC asset layer sprite load result for {spriteName}: state={DescribeState(state)}.");
        }
        catch (Exception ex)
        {
            LogHelper.LogRuntimeDebug($"Custom Workers ARC asset layer failed to create sprite {spriteName}: {ex}");
            cache = null;
            state = AssetLoadState.Failed;
            logger?.LogError($"Custom Workers ARC asset layer threw while creating sprite {spriteName}: {ex}");
        }

        return cache;
    }

    private static string DescribeState(AssetLoadState state)
    {
        switch (state)
        {
            case AssetLoadState.Loaded:
                return "loaded";
            case AssetLoadState.Failed:
                return "failed";
            default:
                return "notLoaded";
        }
    }

    private static bool IsArcBlue(Color color)
    {
        return Approximately(color.r, 22f / 255f)
            && Approximately(color.g, 169f / 255f)
            && Approximately(color.b, 1f)
            && Approximately(color.a, 1f);
    }

    private static bool Approximately(float left, float right)
    {
        return Mathf.Abs(left - right) <= 0.02f;
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
}

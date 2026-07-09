using System.IO;
using BepInEx;
using UnityEngine;

namespace CustomWorkers;

internal static class WorkerIconRules
{
    private const string IconFileName = "worker-profile-icon.png";

    private static Sprite? cachedGeneratedWorkerSprite;
    private static bool attemptedLoad;

    internal static void ResetCache()
    {
        attemptedLoad = false;
        cachedGeneratedWorkerSprite = null;
    }

    internal static Sprite? GetGeneratedWorkerIcon(Sprite? fallbackIcon)
    {
        Sprite? customSprite = LoadGeneratedWorkerIcon();
        return customSprite ?? fallbackIcon;
    }

    internal static string GetGeneratedWorkerIconPath()
    {
        string pluginPath = string.IsNullOrWhiteSpace(Paths.PluginPath)
            ? System.AppContext.BaseDirectory
            : Paths.PluginPath;

        return Path.Combine(pluginPath, "CustomWorkers", IconFileName);
    }

    private static Sprite? LoadGeneratedWorkerIcon()
    {
        if (attemptedLoad)
        {
            return cachedGeneratedWorkerSprite;
        }

        attemptedLoad = true;
        cachedGeneratedWorkerSprite = LoadEmbeddedWorkerIcon();
        return cachedGeneratedWorkerSprite;
    }

    private static Sprite? LoadEmbeddedWorkerIcon()
    {
        if (!Base64SpriteHelper.TryDecodePng(WorkerIconBase64Data.Value, out byte[]? pngBytes, "generated-worker-icon-embedded") || pngBytes == null)
        {
            return null;
        }

        try
        {
            return Base64SpriteHelper.CreateSpriteFromPng(pngBytes, "CustomWorkers_GeneratedWorkerIcon");
        }
        catch (System.Exception)
        {
            return null;
        }
    }
}

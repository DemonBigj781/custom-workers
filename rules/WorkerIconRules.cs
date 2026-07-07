using System.IO;
using BepInEx;
using UnityEngine;

namespace CustomWorkers;

internal static class WorkerIconRules
{
    private const string IconFileName = "worker-profile-icon.png";

    private static Sprite? cachedGeneratedWorkerSprite;
    private static bool attemptedLoad;

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

        string iconPath = GetGeneratedWorkerIconPath();
        if (!File.Exists(iconPath))
        {
            return null;
        }

        byte[] pngBytes = File.ReadAllBytes(iconPath);
        if (pngBytes.Length == 0)
        {
            return null;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
        texture.name = "CustomWorkers_GeneratedWorkerIcon";
        if (!ImageConversion.LoadImage(texture, pngBytes, markNonReadable: false))
        {
            Object.Destroy(texture);
            return null;
        }

        cachedGeneratedWorkerSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        cachedGeneratedWorkerSprite.name = "CustomWorkers_GeneratedWorkerIcon";
        return cachedGeneratedWorkerSprite;
    }
}

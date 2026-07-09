using System;
using UnityEngine;

namespace CustomWorkers;

internal static class Base64SpriteHelper
{
    internal static bool TryDecodePng(string? encoded, out byte[]? pngBytes)
    {
        return TryDecodePng(encoded, out pngBytes, "unknown-base64-source");
    }

    internal static bool TryDecodePng(string? encoded, out byte[]? pngBytes, string sourceName)
    {
        pngBytes = null;
        if (string.IsNullOrWhiteSpace(encoded))
        {
            LogHelper.LogRuntimeDebug($"Custom Workers base64 decode skipped for {sourceName}: source string was empty.");
            return false;
        }

        try
        {
            string trimmed = encoded!.Trim();
            byte[] decodedBytes = Convert.FromBase64String(trimmed);
            pngBytes = decodedBytes;
            LogHelper.LogRuntimeDebug($"Custom Workers base64 decode succeeded for {sourceName}: {decodedBytes.Length} bytes.");
            return decodedBytes.Length > 0;
        }
        catch (FormatException)
        {
            LogHelper.LogRuntimeDebug($"Custom Workers base64 decode failed for {sourceName}: invalid base64 payload.");
            return false;
        }
    }

    internal static Sprite? CreateSpriteFromPng(byte[] pngBytes, string spriteName)
    {
        if (pngBytes == null || pngBytes.Length == 0)
        {
            LogHelper.LogRuntimeDebug("Custom Workers sprite creation skipped: PNG byte array was empty.");
            return null;
        }

        LogHelper.LogRuntimeDebug($"Custom Workers creating sprite '{spriteName}' from {pngBytes.Length} bytes.");
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
        texture.name = spriteName;
        if (!ImageConversion.LoadImage(texture, pngBytes, markNonReadable: false))
        {
            UnityEngine.Object.Destroy(texture);
            LogHelper.LogRuntimeDebug($"Custom Workers sprite creation failed for '{spriteName}'.");
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.name = spriteName;
        LogHelper.LogRuntimeDebug($"Custom Workers sprite creation succeeded for '{spriteName}' ({texture.width}x{texture.height}).");
        return sprite;
    }
}

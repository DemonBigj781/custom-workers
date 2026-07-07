using System;
using UnityEngine;

namespace CustomWorkers;

internal static class ShirtColorRules
{
    internal static bool IsColorableClothingLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return false;
        }

        string normalized = label!.Trim().ToLowerInvariant();
        return normalized.Contains("shirt")
            || normalized.Contains("top")
            || normalized.Contains("torso")
            || normalized.Contains("upper")
            || normalized.Contains("pant")
            || normalized.Contains("trouser")
            || normalized.Contains("shoe")
            || normalized.Contains("boot")
            || normalized.Contains("sneaker")
            || normalized.Contains("sock");
    }

    internal static Color CreateRandomBaseColor()
    {
        return UnityEngine.Random.ColorHSV(0f, 1f, 0.55f, 0.9f, 0.55f, 0.95f, 1f, 1f);
    }

    internal static Color CreateRandomBaseColor(System.Random random)
    {
        float hue = (float)random.NextDouble();
        float saturation = 0.55f + ((float)random.NextDouble() * 0.35f);
        float value = 0.55f + ((float)random.NextDouble() * 0.4f);
        return Color.HSVToRGB(hue, saturation, value);
    }

    internal static Color[] BuildTintSet(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float hue, out float saturation, out float value);

        Color main = Color.HSVToRGB(hue, saturation, value);
        Color tintR = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.78f), Mathf.Clamp01(value * 1.05f));
        Color tintG = Color.HSVToRGB(Mathf.Repeat(hue + 0.03f, 1f), Mathf.Clamp01(saturation * 0.92f), Mathf.Clamp01(value * 0.88f));
        Color tintB = Color.HSVToRGB(Mathf.Repeat(hue - 0.03f, 1f), Mathf.Clamp01(saturation * 0.86f), Mathf.Clamp01(value * 0.72f));

        main.a = 1f;
        tintR.a = 1f;
        tintG.a = 1f;
        tintB.a = 1f;
        return new[] { main, tintR, tintG, tintB };
    }
}

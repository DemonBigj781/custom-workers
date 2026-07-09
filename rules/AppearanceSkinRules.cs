using System;
using UnityEngine;

namespace CustomWorkers;

internal enum AppearanceSkinMode
{
    Off,
    Simpsons,
    Random,
    Eiffel65,
    BlueMen,
    FBI
}

internal enum AppearanceGenderFilter
{
    Both,
    BoysOnly,
    GirlsOnly
}

internal static class AppearanceSkinRules
{
    internal static bool MatchesGender(AppearanceSkinMode mode, AppearanceGenderFilter filter, bool isFemale)
    {
        if (mode == AppearanceSkinMode.BlueMen && isFemale)
        {
            return false;
        }

        return filter switch
        {
            AppearanceGenderFilter.Both => true,
            AppearanceGenderFilter.BoysOnly => !isFemale,
            AppearanceGenderFilter.GirlsOnly => isFemale,
            _ => true
        };
    }

    internal static Color ResolveSkinColor(AppearanceSkinMode mode, System.Random random, Color fallback)
    {
        return mode switch
        {
            AppearanceSkinMode.Simpsons => new Color32(255, 217, 15, 255),
            AppearanceSkinMode.Eiffel65 => new Color32(0, 102, 255, 255),
            AppearanceSkinMode.BlueMen => new Color32(0, 76, 204, 255),
            AppearanceSkinMode.Random => Color.HSVToRGB((float)random.NextDouble(), 0.25f + ((float)random.NextDouble() * 0.55f), 0.75f + ((float)random.NextDouble() * 0.2f)),
            _ => fallback
        };
    }

    internal static bool TriesToOverrideOtherFeatures(AppearanceSkinMode mode)
    {
        return mode == AppearanceSkinMode.Eiffel65
            || mode == AppearanceSkinMode.BlueMen
            || mode == AppearanceSkinMode.FBI;
    }

    internal static bool TryGetForcedHairColor(AppearanceSkinMode mode, out Color color)
    {
        color = Color.white;
        switch (mode)
        {
            case AppearanceSkinMode.Eiffel65:
                color = new Color32(0, 102, 255, 255);
                return true;
            case AppearanceSkinMode.BlueMen:
                color = new Color32(0, 0, 0, 255);
                return true;
            default:
                return false;
        }
    }

    internal static bool TryGetForcedClothingColor(AppearanceSkinMode mode, AppearanceShufflePart part, out Color color)
    {
        color = Color.white;
        switch (mode)
        {
            case AppearanceSkinMode.Eiffel65:
                color = new Color32(0, 102, 255, 255);
                return true;
            case AppearanceSkinMode.BlueMen:
                color = new Color32(0, 0, 0, 255);
                return true;
            default:
                return false;
        }
    }

    internal static bool HidesHair(AppearanceSkinMode mode)
    {
        return mode == AppearanceSkinMode.BlueMen;
    }

    internal static bool AppliesGreenGlow(AppearanceSkinMode mode)
    {
        return mode == AppearanceSkinMode.FBI;
    }

    internal static bool IsSkinObjectName(string? objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        string normalized = objectName!.Trim().ToLowerInvariant();
        return normalized.Contains("skin")
            || normalized.Contains("body")
            || normalized.Contains("face")
            || normalized.Contains("head")
            || normalized.Contains("arm")
            || normalized.Contains("leg")
            || normalized.Contains("hand")
            || normalized.Contains("ear")
            || normalized.Contains("neck");
    }
}

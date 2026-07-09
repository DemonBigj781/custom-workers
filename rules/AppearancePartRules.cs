using System;

namespace CustomWorkers;

internal static class AppearancePartRules
{
    internal static bool TryGetClothingPart(string? label, out AppearanceShufflePart part)
    {
        part = AppearanceShufflePart.Shirt;
        if (string.IsNullOrWhiteSpace(label))
        {
            return false;
        }

        string normalized = label!.Trim().ToLowerInvariant();
        if (normalized.Contains("shirt") || normalized.Contains("top") || normalized.Contains("torso") || normalized.Contains("upper"))
        {
            part = AppearanceShufflePart.Shirt;
            return true;
        }

        if (normalized.Contains("pant") || normalized.Contains("trouser") || normalized.Contains("jean") || normalized.Contains("short"))
        {
            part = AppearanceShufflePart.Pants;
            return true;
        }

        if (normalized.Contains("shoe") || normalized.Contains("boot") || normalized.Contains("sneaker") || normalized.Contains("sock"))
        {
            part = AppearanceShufflePart.Shoes;
            return true;
        }

        return false;
    }

    internal static bool IsHairObjectName(string? objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        string normalized = objectName!.Trim().ToLowerInvariant();
        return normalized.Contains("hair")
            || normalized.Contains("brow")
            || normalized.Contains("beard")
            || normalized.Contains("mustache");
    }
}

using UnityEngine;

namespace CustomWorkers;

internal static class ArcRecruiterHighlightAssets
{
    private static Sprite? cachedControllerHighlightSprite;

    internal static Sprite? GetControllerHighlightSprite()
    {
        if (cachedControllerHighlightSprite != null)
        {
            return cachedControllerHighlightSprite;
        }

        if (!Base64SpriteHelper.TryDecodePng(CtrlbtnselecthighlightBase64Data.Value, out byte[]? pngBytes, "arc-recruiter-controller-highlight-embedded") || pngBytes == null)
        {
            return null;
        }

        cachedControllerHighlightSprite = Base64SpriteHelper.CreateSpriteFromPng(pngBytes, "CustomWorkers_ArcRecruiterControllerHighlight");
        return cachedControllerHighlightSprite;
    }
}

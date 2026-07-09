using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using CC;

namespace CustomWorkers;

internal static class AppearanceHelper
{
    internal static void ApplyCustomerAppearanceShuffling(Customer? customer, FieldInfo apparelObjectsField, AppearanceShuffleOptions options)
    {
        if (!options.EnableCustomerAppearancePatch)
        {
            return;
        }

        if (customer?.m_CharacterCustom == null || !AppearanceShuffleRules.AnyEnabled(options, AppearanceShuffleTarget.Customers))
        {
            if (customer?.m_CharacterCustom == null)
            {
                return;
            }
        }

        int seed = unchecked(WorkerRosterRules.Seed + 300007 + (customer.GetCustomerModelIndex() * 48611) + (customer.gameObject.GetInstanceID() * 97));
        ApplyAppearanceShuffling(customer.m_CharacterCustom, apparelObjectsField, new System.Random(seed), AppearanceShuffleTarget.Customers, options, customer.m_IsFemale);
    }

    internal static void ApplyAppearanceShuffling(CharacterCustomization? customization, FieldInfo apparelObjectsField, System.Random random, AppearanceShuffleTarget target, AppearanceShuffleOptions options, bool isFemale)
    {
        if (customization == null || apparelObjectsField == null || !customization.m_HasInit)
        {
            return;
        }

        IList? apparelObjects = apparelObjectsField.GetValue(customization) as IList;
        if (customization.ApparelTables == null || apparelObjects == null || apparelObjects.Count == 0)
        {
            return;
        }

        AppearanceSkinMode skinMode = AppearanceShuffleRules.GetSkinMode(options, target);
        AppearanceGenderFilter skinFilter = AppearanceShuffleRules.GetSkinGenderFilter(options, target);
        bool skinModeApplies = skinMode != AppearanceSkinMode.Off && AppearanceSkinRules.MatchesGender(skinMode, skinFilter, isFemale);
        bool overrideOtherFeatures = skinModeApplies && AppearanceSkinRules.TriesToOverrideOtherFeatures(skinMode);

        if (AppearanceShuffleRules.IsEnabled(options, target, AppearanceShufflePart.Hair) && !overrideOtherFeatures)
        {
            ApplyNaturalHairColor(customization.gameObject, CreateNaturalHairColor(random));
        }

        if (skinModeApplies)
        {
            ApplySkinColor(customization.gameObject, AppearanceSkinRules.ResolveSkinColor(skinMode, random, new Color32(232, 190, 172, 255)));
            if (AppearanceSkinRules.TryGetForcedHairColor(skinMode, out Color forcedHairColor))
            {
                ApplyNaturalHairColor(customization.gameObject, forcedHairColor);
            }

            ApplyHairVisibility(customization.gameObject, !AppearanceSkinRules.HidesHair(skinMode));
            if (AppearanceSkinRules.AppliesGreenGlow(skinMode))
            {
                ApplyGlowColor(customization.gameObject, new Color32(48, 255, 96, 255), isEnabled: true);
            }
            else
            {
                ApplyGlowColor(customization.gameObject, Color.black, isEnabled: false);
            }
        }
        else
        {
            ApplyHairVisibility(customization.gameObject, visible: true);
            ApplyGlowColor(customization.gameObject, Color.black, isEnabled: false);
        }

        bool applied = false;
        for (int slotIndex = 0; slotIndex < customization.ApparelTables.Count && slotIndex < apparelObjects.Count; slotIndex++)
        {
            if (apparelObjects[slotIndex] is not GameObject apparelObject || apparelObject == null)
            {
                continue;
            }

            if (!AppearancePartRules.TryGetClothingPart(customization.ApparelTables[slotIndex]?.Label, out AppearanceShufflePart part))
            {
                continue;
            }

            if (!overrideOtherFeatures && !AppearanceShuffleRules.IsEnabled(options, target, part))
            {
                continue;
            }

            Color[] slotTints = overrideOtherFeatures && AppearanceSkinRules.TryGetForcedClothingColor(skinMode, part, out Color forcedClothingColor)
                ? ShirtColorRules.BuildTintSet(forcedClothingColor)
                : ShirtColorRules.BuildTintSet(ShirtColorRules.CreateRandomBaseColor(random));
            applied |= ApplyTintsToObject(apparelObject, slotTints, part);
        }

        if (!applied)
        {
            for (int slotIndex = 0; slotIndex < apparelObjects.Count; slotIndex++)
            {
                if (apparelObjects[slotIndex] is not GameObject apparelObject || apparelObject == null)
                {
                    continue;
                }

                Color[] slotTints = ShirtColorRules.BuildTintSet(ShirtColorRules.CreateRandomBaseColor(random));
                if (overrideOtherFeatures && AppearancePartRules.TryGetClothingPart(apparelObject.name, out AppearanceShufflePart part) && AppearanceSkinRules.TryGetForcedClothingColor(skinMode, part, out Color forcedClothingColor))
                {
                    slotTints = ShirtColorRules.BuildTintSet(forcedClothingColor);
                }

                ApplyTintsToObject(apparelObject, slotTints, enabledParts: options, target: target, requireObjectClassification: true);
            }
        }
    }

    private static Color CreateNaturalHairColor(System.Random random)
    {
        Color[] naturalPalette =
        {
            new Color32(32, 23, 17, 255),
            new Color32(58, 41, 31, 255),
            new Color32(86, 58, 43, 255),
            new Color32(122, 87, 62, 255),
            new Color32(161, 123, 84, 255),
            new Color32(196, 170, 120, 255),
            new Color32(214, 195, 155, 255),
            new Color32(72, 72, 68, 255),
            new Color32(122, 122, 118, 255)
        };

        return naturalPalette[random.Next(naturalPalette.Length)];
    }

    private static void ApplyNaturalHairColor(GameObject rootObject, Color hairColor)
    {
        Color[] hairTints = ShirtColorRules.BuildTintSet(hairColor);
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null || !AppearancePartRules.IsHairObjectName(renderer.gameObject.name))
            {
                continue;
            }

            ApplyTintsToMaterials(renderer.materials, hairTints);
        }
    }

    private static void ApplySkinColor(GameObject rootObject, Color skinColor)
    {
        Color[] skinTints = ShirtColorRules.BuildTintSet(skinColor);
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null || !AppearanceSkinRules.IsSkinObjectName(renderer.gameObject.name))
            {
                continue;
            }

            ApplyTintsToMaterials(renderer.materials, skinTints);
        }
    }

    private static void ApplyHairVisibility(GameObject rootObject, bool visible)
    {
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null || !AppearancePartRules.IsHairObjectName(renderer.gameObject.name))
            {
                continue;
            }

            renderer.enabled = visible;
        }
    }

    private static void ApplyGlowColor(GameObject rootObject, Color glowColor, bool isEnabled)
    {
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.materials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null || !material.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                material.SetColor("_EmissionColor", isEnabled ? glowColor : Color.black);
                if (isEnabled)
                {
                    material.EnableKeyword("_EMISSION");
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    private static bool ApplyTintsToObject(GameObject apparelObject, Color[] tints, AppearanceShufflePart part)
    {
        return ApplyTintsToObject(apparelObject, tints, enabledParts: null, target: AppearanceShuffleTarget.Customers, requireObjectClassification: false, forcedPart: part);
    }

    private static bool ApplyTintsToObject(GameObject apparelObject, Color[] tints, AppearanceShuffleOptions? enabledParts = null, AppearanceShuffleTarget target = AppearanceShuffleTarget.Customers, bool requireObjectClassification = false, AppearanceShufflePart? forcedPart = null)
    {
        bool applied = false;
        SkinnedMeshRenderer[] renderers = apparelObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            AppearanceShufflePart part = forcedPart ?? AppearanceShufflePart.Shirt;
            if (forcedPart == null)
            {
                if (!AppearancePartRules.TryGetClothingPart(renderer.gameObject.name, out part))
                {
                    if (requireObjectClassification)
                    {
                        continue;
                    }

                    part = AppearanceShufflePart.Shirt;
                }
            }

            if (enabledParts != null && !AppearanceShuffleRules.IsEnabled(enabledParts, target, part))
            {
                continue;
            }

            applied |= ApplyTintsToMaterials(renderer.materials, tints);
        }

        return applied;
    }

    private static bool ApplyTintsToMaterials(Material[] materials, Color[] tints)
    {
        bool applied = false;
        for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
        {
            Material material = materials[materialIndex];
            if (material == null)
            {
                continue;
            }

            bool materialApplied = false;
            if (material.HasProperty("_Tint"))
            {
                material.SetColor("_Tint", tints[0]);
                materialApplied = true;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tints[0]);
                materialApplied = true;
            }

            if (material.HasProperty("_Tint_R"))
            {
                material.SetColor("_Tint_R", tints[1]);
                materialApplied = true;
            }

            if (material.HasProperty("_Tint_G"))
            {
                material.SetColor("_Tint_G", tints[2]);
                materialApplied = true;
            }

            if (material.HasProperty("_Tint_B"))
            {
                material.SetColor("_Tint_B", tints[3]);
                materialApplied = true;
            }

            applied |= materialApplied;
        }

        return applied;
    }
}

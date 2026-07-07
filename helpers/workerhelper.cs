using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using CC;

namespace CustomWorkers;

internal sealed class GeneratedWorkerAppearance
{
    internal bool IsFemale;
    internal int CharacterModelIndex;
    internal Vector3 CharacterScale;
    internal Color HairColor;
}

internal static class WorkerHelper
{
    internal static bool TryGetGeneratedWorkerAppearance(Worker? worker, IDictionary<int, GeneratedWorkerAppearance> appearances, out GeneratedWorkerAppearance? appearance)
    {
        appearance = null;
        if (worker == null || worker.m_WorkerIndex < 0)
        {
            return false;
        }

        return appearances.TryGetValue(worker.m_WorkerIndex, out appearance);
    }

    internal static GeneratedWorkerAppearance BuildGeneratedWorkerAppearance(int workerIndex, int generatedSlotIndex, FieldInfo maxFemaleModelIndexField, FieldInfo maxMaleModelIndexField)
    {
        var random = new System.Random(unchecked(WorkerRosterRules.Seed + 100003 + (workerIndex * 48611) + (generatedSlotIndex * 97)));
        bool isFemale = random.Next(2) == 0;
        int maxModelIndex = GetCustomerModelIndexMax(isFemale, maxFemaleModelIndexField, maxMaleModelIndexField);
        int characterModelIndex = maxModelIndex > 0 ? random.Next(maxModelIndex + 1) : 0;

        float heightScale = 0.9f + ((float)random.NextDouble() * 0.25f);
        float widthScale = 0.9f + ((float)random.NextDouble() * 0.2f);
        float depthScale = 0.9f + ((float)random.NextDouble() * 0.2f);

        return new GeneratedWorkerAppearance
        {
            IsFemale = isFemale,
            CharacterModelIndex = characterModelIndex,
            CharacterScale = new Vector3(widthScale, heightScale, depthScale),
            HairColor = CreateNaturalHairColor(random)
        };
    }

    internal static void ApplyGeneratedWorkerClothingColors(Worker? worker, FieldInfo apparelObjectsField, IDictionary<int, GeneratedWorkerAppearance> appearances)
    {
        if (worker?.m_CharacterCustom == null || apparelObjectsField == null || !TryGetGeneratedWorkerAppearance(worker, appearances, out GeneratedWorkerAppearance? appearance))
        {
            return;
        }

        CharacterCustomization customization = worker.m_CharacterCustom;
        if (!customization.m_HasInit || customization.ApparelTables == null || customization.ApparelTables.Count == 0)
        {
            return;
        }

        IList? apparelObjects = apparelObjectsField.GetValue(customization) as IList;
        if (apparelObjects == null || apparelObjects.Count == 0)
        {
            return;
        }

        worker.m_CharacterCustom.transform.localScale = appearance.CharacterScale;
        ApplyNaturalHairColor(worker.m_CharacterCustom.gameObject, appearance.HairColor);

        bool applied = false;
        var colorRandom = new System.Random(unchecked(WorkerRosterRules.Seed + (worker.m_WorkerIndex * 48611)));

        for (int slotIndex = 0; slotIndex < customization.ApparelTables.Count && slotIndex < apparelObjects.Count; slotIndex++)
        {
            if (!ShirtColorRules.IsColorableClothingLabel(customization.ApparelTables[slotIndex]?.Label))
            {
                continue;
            }

            if (apparelObjects[slotIndex] is not GameObject apparelObject || apparelObject == null)
            {
                continue;
            }

            Color[] slotTints = ShirtColorRules.BuildTintSet(ShirtColorRules.CreateRandomBaseColor(colorRandom));
            applied |= ApplyTintsToObject(apparelObject, slotTints);
        }

        if (!applied)
        {
            for (int slotIndex = 0; slotIndex < apparelObjects.Count; slotIndex++)
            {
                if (apparelObjects[slotIndex] is not GameObject apparelObject || apparelObject == null)
                {
                    continue;
                }

                Color[] slotTints = ShirtColorRules.BuildTintSet(ShirtColorRules.CreateRandomBaseColor(colorRandom));
                if (ApplyTintsToObject(apparelObject, slotTints, requireClothingName: true))
                {
                    applied = true;
                }
            }
        }
    }

    private static int GetCustomerModelIndexMax(bool isFemale, FieldInfo maxFemaleModelIndexField, FieldInfo maxMaleModelIndexField)
    {
        FieldInfo? field = isFemale ? maxFemaleModelIndexField : maxMaleModelIndexField;
        if (field?.GetValue(CSingleton<CustomerManager>.Instance) is int count)
        {
            return count;
        }

        return isFemale ? 15 : 35;
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
        if (rootObject == null)
        {
            return;
        }

        Color[] hairTints = ShirtColorRules.BuildTintSet(hairColor);
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null)
            {
                continue;
            }

            string objectName = renderer.gameObject.name.ToLowerInvariant();
            if (!objectName.Contains("hair") && !objectName.Contains("brow") && !objectName.Contains("beard") && !objectName.Contains("mustache"))
            {
                continue;
            }

            Material[] materials = renderer.materials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty("_Tint"))
                {
                    material.SetColor("_Tint", hairTints[0]);
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", hairTints[0]);
                }

                if (material.HasProperty("_Tint_R"))
                {
                    material.SetColor("_Tint_R", hairTints[1]);
                }

                if (material.HasProperty("_Tint_G"))
                {
                    material.SetColor("_Tint_G", hairTints[2]);
                }

                if (material.HasProperty("_Tint_B"))
                {
                    material.SetColor("_Tint_B", hairTints[3]);
                }
            }
        }
    }

    private static bool ApplyTintsToObject(GameObject apparelObject, Color[] tints, bool requireClothingName = false)
    {
        bool applied = false;
        SkinnedMeshRenderer[] renderers = apparelObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            string objectName = renderer.gameObject.name.ToLowerInvariant();
            if (requireClothingName && !ShirtColorRules.IsColorableClothingLabel(objectName))
            {
                continue;
            }

            Material[] materials = renderer.materials;
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
        }

        return applied;
    }
}

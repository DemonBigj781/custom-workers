using System;
using System.Collections.Generic;
using System.Reflection;
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
    private static readonly HashSet<int> ValidFemaleModelIndices = new HashSet<int>();
    private static readonly HashSet<int> ValidMaleModelIndices = new HashSet<int>();
    private static readonly HashSet<int> InvalidFemaleModelIndices = new HashSet<int>();
    private static readonly HashSet<int> InvalidMaleModelIndices = new HashSet<int>();

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
        int characterModelIndex = ChooseValidatedModelIndex(isFemale, maxModelIndex, random);

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

    internal static void ObserveGeneratedWorkerModelResult(Worker? worker, IDictionary<int, GeneratedWorkerAppearance> appearances)
    {
        if (!TryGetGeneratedWorkerAppearance(worker, appearances, out GeneratedWorkerAppearance? appearance) || worker?.m_CharacterCustom?.StoredCharacterData == null)
        {
            return;
        }

        GeneratedWorkerAppearance nonNullAppearance = appearance!;
        string observedBodyType = worker.m_CharacterCustom.StoredCharacterData.CharacterPrefab ?? string.Empty;
        bool matches = IsObservedBodyTypeCompatible(nonNullAppearance.IsFemale, observedBodyType);
        HashSet<int> validSet = nonNullAppearance.IsFemale ? ValidFemaleModelIndices : ValidMaleModelIndices;
        HashSet<int> invalidSet = nonNullAppearance.IsFemale ? InvalidFemaleModelIndices : InvalidMaleModelIndices;

        if (matches)
        {
            if (validSet.Add(nonNullAppearance.CharacterModelIndex))
            {
                LogHelper.LogRuntimeDebug($"Custom Workers validated generated worker model index {nonNullAppearance.CharacterModelIndex} for {(nonNullAppearance.IsFemale ? "female" : "male")} bodyType='{observedBodyType}'.");
            }

            invalidSet.Remove(nonNullAppearance.CharacterModelIndex);
        }
        else
        {
            if (invalidSet.Add(nonNullAppearance.CharacterModelIndex))
            {
                LogHelper.LogRuntimeDebug($"Custom Workers invalidated generated worker model index {nonNullAppearance.CharacterModelIndex} for {(nonNullAppearance.IsFemale ? "female" : "male")} bodyType='{observedBodyType}'.");
            }
        }
    }

    internal static string GetFallbackWorkerCharacterName()
    {
        WorkerData? workerData = WorkerManager.GetWorkerData(0);
        if (workerData == null)
        {
            return "Worker0";
        }

        return "Worker0";
    }

    internal static void ApplyGeneratedWorkerClothingColors(Worker? worker, FieldInfo apparelObjectsField, IDictionary<int, GeneratedWorkerAppearance> appearances, AppearanceShuffleOptions options)
    {
        if (!options.EnableWorkerAppearancePatch)
        {
            return;
        }

        if (worker?.m_CharacterCustom == null || apparelObjectsField == null || !TryGetGeneratedWorkerAppearance(worker, appearances, out GeneratedWorkerAppearance? appearance))
        {
            return;
        }

        CharacterCustomization customization = worker.m_CharacterCustom;
        if (!customization.m_HasInit)
        {
            return;
        }

        CharacterCustomization nonNullCustomization = worker.m_CharacterCustom;
        GeneratedWorkerAppearance nonNullAppearance = appearance!;
        nonNullCustomization.transform.localScale = nonNullAppearance.CharacterScale;
        var colorRandom = new System.Random(unchecked(WorkerRosterRules.Seed + (worker.m_WorkerIndex * 48611)));
        AppearanceHelper.ApplyAppearanceShuffling(customization, apparelObjectsField, colorRandom, AppearanceShuffleTarget.Workers, options, nonNullAppearance.IsFemale);
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

    private static int ChooseValidatedModelIndex(bool isFemale, int maxModelIndex, System.Random random)
    {
        if (maxModelIndex <= 0)
        {
            return 0;
        }

        HashSet<int> validSet = isFemale ? ValidFemaleModelIndices : ValidMaleModelIndices;
        HashSet<int> invalidSet = isFemale ? InvalidFemaleModelIndices : InvalidMaleModelIndices;
        List<int> candidates = new List<int>();

        if (validSet.Count > 0)
        {
            foreach (int index in validSet)
            {
                if (index >= 0 && index <= maxModelIndex)
                {
                    candidates.Add(index);
                }
            }
        }
        else
        {
            for (int index = 0; index <= maxModelIndex; index++)
            {
                if (!invalidSet.Contains(index))
                {
                    candidates.Add(index);
                }
            }
        }

        if (candidates.Count == 0)
        {
            for (int index = 0; index <= maxModelIndex; index++)
            {
                candidates.Add(index);
            }
        }

        return candidates[random.Next(candidates.Count)];
    }

    private static bool IsObservedBodyTypeCompatible(bool expectedFemale, string observedBodyType)
    {
        if (string.IsNullOrWhiteSpace(observedBodyType))
        {
            return false;
        }

        bool saysFemale = observedBodyType.StartsWith("Female_", StringComparison.OrdinalIgnoreCase)
            || string.Equals(observedBodyType, "Female", StringComparison.OrdinalIgnoreCase);
        bool saysMale = observedBodyType.StartsWith("Male_", StringComparison.OrdinalIgnoreCase)
            || string.Equals(observedBodyType, "Male", StringComparison.OrdinalIgnoreCase);
        if (expectedFemale)
        {
            return saysFemale && !saysMale;
        }

        return saysMale && !saysFemale;
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

}

using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomWorkers;

internal static class WorkerRosterRules
{
    internal const int Seed = 1337;

    private static readonly string[] OrderedFemaleNames = BuildOrderedDistinct(FirstNamePools.Female);
    private static readonly string[] OrderedMaleNames = BuildOrderedDistinct(FirstNamePools.Male);
    private static readonly string[] OrderedMixedNames = BuildOrderedDistinct(FirstNamePools.International);

    internal static int GetGeneratedWorkerCount()
    {
        return 7;
    }

    internal static WorkerData CreateGeneratedWorkerData(WorkerData template, int newIndex, int generatedSlotIndex, bool isFemale, ISet<string> usedNames)
    {
        Random random = CreateRandom(newIndex, isFemale);
        string name = NextUniqueName(random, generatedSlotIndex, isFemale, usedNames);

        float restockFactor = NextRange(random, 0.88f, 1.12f);
        float checkoutFactor = NextRange(random, 0.88f, 1.12f);
        float walkBonus = NextRange(random, -0.05f, 0.15f);
        float overallStrength = (2f / restockFactor + 2f / checkoutFactor + (1f + walkBonus)) / 3f;
        float payFactor = Math.Max(0.85f, Math.Min(1.35f, overallStrength));

        return new WorkerData
        {
            name = name,
            icon = WorkerIconRules.GetGeneratedWorkerIcon(template.icon),
            restockSpeed = Math.Max(0.1f, template.restockSpeed * restockFactor),
            checkoutSpeed = Math.Max(0.1f, template.checkoutSpeed * checkoutFactor),
            walkSpeedMultiplier = Math.Max(0f, template.walkSpeedMultiplier + walkBonus),
            costPerDay = DailyCostForGeneratedSlot(generatedSlotIndex),
            hiringCost = 2000f,
            arriveEarlySpeedMin = template.arriveEarlySpeedMin,
            arriveEarlySpeedMax = template.arriveEarlySpeedMax,
            shopLevelRequired = 1,
            description = WorkerBioRules.BuildRawDescription(newIndex, isFemale),
            bonusConversation = WorkerBioRules.BuildRawBonusConversation(newIndex, isFemale),
            goBackOnTime = template.goBackOnTime,
            prologueShow = template.prologueShow
        };
    }

    internal static HashSet<string> CollectUsedNames(IEnumerable<WorkerData> workers)
    {
        return new HashSet<string>(workers
            .Select(static worker => worker?.name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))!
            .Select(static name => name!.Trim()), StringComparer.OrdinalIgnoreCase);
    }

    private static Random CreateRandom(int workerIndex, bool isFemale)
    {
        int salt = isFemale ? 17 : 29;
        return new Random(unchecked(Seed + salt + (workerIndex * 7919)));
    }

    private static string NextUniqueName(Random random, int generatedSlotIndex, bool isFemale, ISet<string> usedNames)
    {
        string[] preferredNames = isFemale ? OrderedFemaleNames : OrderedMaleNames;
        string[] secondaryNames = OrderedMixedNames;
        string[] tertiaryNames = isFemale ? OrderedMaleNames : OrderedFemaleNames;

        string? match = TryPickOrderedUniqueFullName(random, generatedSlotIndex, preferredNames, usedNames)
            ?? TryPickOrderedUniqueFullName(random, generatedSlotIndex, secondaryNames, usedNames)
            ?? TryPickOrderedUniqueFullName(random, generatedSlotIndex, tertiaryNames, usedNames)
            ?? TryPickUniqueFullName(random, isFemale ? FirstNamePools.Female : FirstNamePools.Male, usedNames)
            ?? TryPickUniqueFullName(random, FirstNamePools.International, usedNames)
            ?? TryPickUniqueFullName(random, isFemale ? FirstNamePools.Male : FirstNamePools.Female, usedNames)
            ?? TryPickUniqueFullName(random, preferredNames, usedNames)
            ?? TryPickUniqueFullName(random, secondaryNames, usedNames)
            ?? TryPickUniqueFullName(random, tertiaryNames, usedNames);

        if (match != null)
        {
            usedNames.Add(match);
            return match;
        }

        int suffix = 1;
        while (usedNames.Contains($"Worker-{Seed}-{suffix}"))
        {
            suffix++;
        }

        string generated = $"Worker-{Seed}-{suffix}";
        usedNames.Add(generated);
        return generated;
    }

    private static string? TryPickOrderedUniqueFullName(Random random, int generatedSlotIndex, string[] firstNames, ISet<string> usedNames)
    {
        if (firstNames.Length == 0 || LastNamePools.Common.Length == 0)
        {
            return null;
        }

        int firstStart = Modulo(generatedSlotIndex, firstNames.Length);
        int lastStart = random.Next(LastNamePools.Common.Length);

        for (int firstOffset = 0; firstOffset < firstNames.Length; firstOffset++)
        {
            string firstName = firstNames[(firstStart + firstOffset) % firstNames.Length];
            for (int lastOffset = 0; lastOffset < LastNamePools.Common.Length; lastOffset++)
            {
                string lastName = LastNamePools.Common[(lastStart + lastOffset) % LastNamePools.Common.Length];
                string candidate = $"{firstName} {lastName}";
                if (!usedNames.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string? TryPickUniqueFullName(Random random, string[] firstNames, ISet<string> usedNames)
    {
        if (firstNames.Length == 0 || LastNamePools.Common.Length == 0)
        {
            return null;
        }

        int firstStart = random.Next(firstNames.Length);
        int lastStart = random.Next(LastNamePools.Common.Length);

        for (int firstOffset = 0; firstOffset < firstNames.Length; firstOffset++)
        {
            string firstName = firstNames[(firstStart + firstOffset) % firstNames.Length];
            for (int lastOffset = 0; lastOffset < LastNamePools.Common.Length; lastOffset++)
            {
                string lastName = LastNamePools.Common[(lastStart + lastOffset) % LastNamePools.Common.Length];
                string candidate = $"{firstName} {lastName}";
                if (!usedNames.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static float NextRange(Random random, float min, float max)
    {
        return min + ((float)random.NextDouble() * (max - min));
    }

    private static string[] BuildOrderedDistinct(IEnumerable<string> source)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();

        AddOrderedDistinct(ordered, seen, source);

        return ordered.ToArray();
    }

    private static void AddOrderedDistinct(List<string> ordered, HashSet<string> seen, IEnumerable<string> source)
    {
        foreach (string name in source)
        {
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
            {
                continue;
            }

            ordered.Add(name);
        }
    }

    private static int Modulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static float RoundCurrency(float value)
    {
        return (float)Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static float DailyCostForGeneratedSlot(int generatedSlotIndex)
    {
        return Modulo(generatedSlotIndex, 3) switch
        {
            0 => 50f,
            1 => 100f,
            _ => 150f
        };
    }
}

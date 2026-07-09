using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomWorkers;

internal static class WorkerRosterRules
{
    internal const int Seed = 1337;

    private static readonly string[] OrderedFemaleNames = BuildOrderedDistinct(FemaleNamePools.First);
    private static readonly string[] OrderedMaleNames = BuildOrderedDistinct(MaleNamePools.First);
    private static readonly string[] OrderedMixedNames = BuildOrderedDistinct(MixedGenderNamePools.First);
    private static readonly string[] OrderedAllFirstNames = BuildOrderedDistinct(OrderedMaleNames.Concat(OrderedFemaleNames).Concat(OrderedMixedNames));
    private static readonly string[] OrderedAllLastNames = BuildOrderedDistinct(LastNamePools.Common);

    internal static int GetGeneratedWorkerCount(IEnumerable<WorkerData> existingWorkers)
    {
        int requested = Math.Max(1, AppearanceSettingsHelper.GetCurrentOptions().GeneratedWorkerCount);
        int maximum = GetMaximumGeneratedWorkerCount(existingWorkers);
        return Math.Min(requested, maximum);
    }

    internal static int GetMaximumGeneratedWorkerCount(IEnumerable<WorkerData> existingWorkers)
    {
        HashSet<string> usedFirstNames = CollectUsedFirstNames(existingWorkers);
        HashSet<string> usedLastNames = CollectUsedLastNames(existingWorkers);
        int availableFirstNames = OrderedAllFirstNames.Count(first => !usedFirstNames.Contains(first));
        int availableLastNames = OrderedAllLastNames.Count(last => !usedLastNames.Contains(last));
        return Math.Max(1, Math.Min(availableFirstNames, availableLastNames));
    }

    internal static WorkerData CreateGeneratedWorkerData(WorkerData template, int newIndex, int generatedSlotIndex, bool isFemale, ISet<string> usedNames, ISet<string> usedFirstNames, ISet<string> usedLastNames)
    {
        Random random = CreateRandom(newIndex, isFemale);
        string name = NextUniqueName(random, generatedSlotIndex, isFemale, usedNames, usedFirstNames, usedLastNames);

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

    internal static HashSet<string> CollectUsedFirstNames(IEnumerable<WorkerData> workers)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (WorkerData worker in workers)
        {
            string? firstName = SplitName(worker?.name).firstName;
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                used.Add(firstName);
            }
        }

        return used;
    }

    internal static HashSet<string> CollectUsedLastNames(IEnumerable<WorkerData> workers)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (WorkerData worker in workers)
        {
            string? lastName = SplitName(worker?.name).lastName;
            if (!string.IsNullOrWhiteSpace(lastName))
            {
                used.Add(lastName);
            }
        }

        return used;
    }

    private static Random CreateRandom(int workerIndex, bool isFemale)
    {
        int salt = isFemale ? 17 : 29;
        return new Random(unchecked(Seed + salt + (workerIndex * 7919)));
    }

    private static string NextUniqueName(Random random, int generatedSlotIndex, bool isFemale, ISet<string> usedNames, ISet<string> usedFirstNames, ISet<string> usedLastNames)
    {
        string[] preferredNames = isFemale ? OrderedFemaleNames : OrderedMaleNames;
        string[] secondaryNames = OrderedMixedNames;
        string[] tertiaryNames = isFemale ? OrderedMaleNames : OrderedFemaleNames;

        string? match = TryPickOrderedUniqueFullName(random, generatedSlotIndex, preferredNames, usedNames, usedFirstNames, usedLastNames)
            ?? TryPickOrderedUniqueFullName(random, generatedSlotIndex, secondaryNames, usedNames, usedFirstNames, usedLastNames)
            ?? TryPickOrderedUniqueFullName(random, generatedSlotIndex, tertiaryNames, usedNames, usedFirstNames, usedLastNames)
            ?? TryPickUniqueFullName(random, isFemale ? FemaleNamePools.First : MaleNamePools.First, usedNames, usedFirstNames, usedLastNames)
            ?? TryPickUniqueFullName(random, MixedGenderNamePools.First, usedNames, usedFirstNames, usedLastNames)
            ?? TryPickUniqueFullName(random, isFemale ? MaleNamePools.First : FemaleNamePools.First, usedNames, usedFirstNames, usedLastNames)
            ?? TryPickUniqueFullName(random, preferredNames, usedNames, usedFirstNames, usedLastNames)
            ?? TryPickUniqueFullName(random, secondaryNames, usedNames, usedFirstNames, usedLastNames)
            ?? TryPickUniqueFullName(random, tertiaryNames, usedNames, usedFirstNames, usedLastNames);

        if (match != null)
        {
            usedNames.Add(match);
            (string firstName, string lastName) = SplitName(match);
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                usedFirstNames.Add(firstName);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                usedLastNames.Add(lastName);
            }

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

    private static string? TryPickOrderedUniqueFullName(Random random, int generatedSlotIndex, string[] firstNames, ISet<string> usedNames, ISet<string> usedFirstNames, ISet<string> usedLastNames)
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
                if (!usedNames.Contains(candidate) && !usedFirstNames.Contains(firstName) && !usedLastNames.Contains(lastName))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string? TryPickUniqueFullName(Random random, string[] firstNames, ISet<string> usedNames, ISet<string> usedFirstNames, ISet<string> usedLastNames)
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
                if (!usedNames.Contains(candidate) && !usedFirstNames.Contains(firstName) && !usedLastNames.Contains(lastName))
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

    private static (string firstName, string lastName) SplitName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (string.Empty, string.Empty);
        }

        string normalizedName = fullName!.Trim();
        string[] parts = normalizedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        return (parts[0], parts.Length > 1 ? parts[parts.Length - 1] : string.Empty);
    }
}

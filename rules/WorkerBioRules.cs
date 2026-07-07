using System;

namespace CustomWorkers;

internal static class WorkerBioRules
{
    internal const string RawTextPrefix = "__raw__";

    internal static string BuildRawDescription(int workerIndex, bool isFemale)
    {
        Random random = CreateRandom(workerIndex, isFemale, 101);
        return RawTextPrefix + Pick(random, DescriptionPools.Primary);
    }

    internal static string BuildRawBonusConversation(int workerIndex, bool isFemale)
    {
        Random random = CreateRandom(workerIndex, isFemale, 202);
        return RawTextPrefix + Pick(random, DescriptionPools.Bonus);
    }

    internal static bool IsRawText(string? value)
    {
        return value != null && value.StartsWith(RawTextPrefix, StringComparison.Ordinal);
    }

    internal static string StripRawPrefix(string value)
    {
        return IsRawText(value) ? value.Substring(RawTextPrefix.Length) : value;
    }

    private static Random CreateRandom(int workerIndex, bool isFemale, int salt)
    {
        int genderSalt = isFemale ? 17 : 29;
        return new Random(unchecked(WorkerRosterRules.Seed + salt + genderSalt + (workerIndex * 7919)));
    }

    private static string Pick(Random random, string[] pool)
    {
        return pool[random.Next(pool.Length)];
    }
}

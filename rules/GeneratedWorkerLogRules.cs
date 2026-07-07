using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CustomWorkers;

internal sealed class GeneratedWorkerLogEntry
{
    internal GeneratedWorkerLogEntry(
        int workerIndex,
        int generatedSlotIndex,
        string name,
        bool isFemale,
        int characterModelIndex,
        (float x, float y, float z) characterScale,
        (byte r, byte g, byte b, byte a) hairColor,
        float costPerDay,
        float hiringCost,
        float restockSpeed,
        float checkoutSpeed,
        float walkSpeedMultiplier,
        string description,
        string bonusConversation)
    {
        WorkerIndex = workerIndex;
        GeneratedSlotIndex = generatedSlotIndex;
        Name = name;
        IsFemale = isFemale;
        CharacterModelIndex = characterModelIndex;
        CharacterScale = characterScale;
        HairColor = hairColor;
        CostPerDay = costPerDay;
        HiringCost = hiringCost;
        RestockSpeed = restockSpeed;
        CheckoutSpeed = checkoutSpeed;
        WalkSpeedMultiplier = walkSpeedMultiplier;
        Description = description;
        BonusConversation = bonusConversation;
    }

    internal int WorkerIndex { get; }
    internal int GeneratedSlotIndex { get; }
    internal string Name { get; }
    internal bool IsFemale { get; }
    internal int CharacterModelIndex { get; }
    internal (float x, float y, float z) CharacterScale { get; }
    internal (byte r, byte g, byte b, byte a) HairColor { get; }
    internal float CostPerDay { get; }
    internal float HiringCost { get; }
    internal float RestockSpeed { get; }
    internal float CheckoutSpeed { get; }
    internal float WalkSpeedMultiplier { get; }
    internal string Description { get; }
    internal string BonusConversation { get; }
}

internal static class GeneratedWorkerLogRules
{
    internal const string FileName = "generated-workers.json";

    internal static string BuildJson(IReadOnlyList<GeneratedWorkerLogEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[");

        for (int index = 0; index < entries.Count; index++)
        {
            GeneratedWorkerLogEntry entry = entries[index];
            builder.Append("  {");
            builder.Append("\"workerIndex\":").Append(entry.WorkerIndex);
            builder.Append(",\"generatedSlotIndex\":").Append(entry.GeneratedSlotIndex);
            builder.Append(",\"name\":\"").Append(Escape(entry.Name)).Append('"');
            builder.Append(",\"isFemale\":").Append(entry.IsFemale ? "true" : "false");
            builder.Append(",\"characterModelIndex\":").Append(entry.CharacterModelIndex);
            builder.Append(",\"characterScale\":{");
            builder.Append("\"x\":").Append(Format(entry.CharacterScale.x));
            builder.Append(",\"y\":").Append(Format(entry.CharacterScale.y));
            builder.Append(",\"z\":").Append(Format(entry.CharacterScale.z)).Append('}');
            builder.Append(",\"hairColor\":{");
            builder.Append("\"r\":").Append(entry.HairColor.r);
            builder.Append(",\"g\":").Append(entry.HairColor.g);
            builder.Append(",\"b\":").Append(entry.HairColor.b);
            builder.Append(",\"a\":").Append(entry.HairColor.a).Append('}');
            builder.Append(",\"costPerDay\":").Append(Format(entry.CostPerDay));
            builder.Append(",\"hiringCost\":").Append(Format(entry.HiringCost));
            builder.Append(",\"restockSpeed\":").Append(Format(entry.RestockSpeed));
            builder.Append(",\"checkoutSpeed\":").Append(Format(entry.CheckoutSpeed));
            builder.Append(",\"walkSpeedMultiplier\":").Append(Format(entry.WalkSpeedMultiplier));
            builder.Append(",\"description\":\"").Append(Escape(entry.Description)).Append('"');
            builder.Append(",\"bonusConversation\":\"").Append(Escape(entry.BonusConversation)).Append('"');
            builder.Append('}');

            if (index + 1 < entries.Count)
            {
                builder.Append(',');
            }

            builder.AppendLine();
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string Format(float value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }
}

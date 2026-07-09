using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CC;
using UnityEngine;

namespace CustomWorkers;

internal static class CustomerRosterDebugRules
{
    internal static string GetFileName(string snapshotName)
    {
        return $"customer-roster-{Sanitize(snapshotName)}.json";
    }

    internal static string BuildJson(IReadOnlyList<Customer> customers)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[");

        for (int index = 0; index < customers.Count; index++)
        {
            Customer customer = customers[index];
            builder.Append("  {");
            builder.Append("\"customerIndex\":").Append(index);
            builder.Append(",\"isFemale\":").Append(customer != null && customer.m_IsFemale ? "true" : "false");
            builder.Append(",\"isActive\":").Append(customer != null && customer.IsActive() ? "true" : "false");
            builder.Append(",\"isInsideShop\":").Append(customer != null && customer.IsInsideShop() ? "true" : "false");

            int modelIndex = -1;
            if (customer != null && PluginAccess.CustomerModelIndexGetter != null)
            {
                object result = PluginAccess.CustomerModelIndexGetter.Invoke(customer, null);
                if (result is int value)
                {
                    modelIndex = value;
                }
            }

            builder.Append(",\"characterModelIndex\":").Append(modelIndex);
            if (customer != null)
            {
                Vector3 position = customer.transform.position;
                builder.Append(",\"position\":{");
                builder.Append("\"x\":").Append(position.x.ToString("R", CultureInfo.InvariantCulture));
                builder.Append(",\"y\":").Append(position.y.ToString("R", CultureInfo.InvariantCulture));
                builder.Append(",\"z\":").Append(position.z.ToString("R", CultureInfo.InvariantCulture)).Append('}');
                builder.Append(",\"currentCostTotal\":").Append(customer.m_CurrentCostTotal.ToString("R", CultureInfo.InvariantCulture));
                AppendCharacterCustomization(builder, customer.m_CharacterCustom);
            }
            else
            {
                builder.Append(",\"position\":null");
                builder.Append(",\"currentCostTotal\":0");
                builder.Append(",\"characterCustomization\":null");
            }

            builder.Append('}');
            if (index + 1 < customers.Count)
            {
                builder.Append(',');
            }

            builder.AppendLine();
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static void AppendCharacterCustomization(StringBuilder builder, CharacterCustomization customization)
    {
        if (customization == null)
        {
            builder.Append(",\"characterCustomization\":null");
            return;
        }

        builder.Append(",\"characterCustomization\":{");
        builder.Append("\"characterName\":\"").Append(Escape(customization.CharacterName ?? string.Empty)).Append('"');
        builder.Append(",\"hasInit\":").Append(customization.m_HasInit ? "true" : "false");

        CC_CharacterData stored = customization.StoredCharacterData;
        if (stored != null)
        {
            builder.Append(",\"bodyType\":\"").Append(Escape(stored.CharacterPrefab ?? string.Empty)).Append('"');
            builder.Append(",\"hairStyles\":");
            AppendStringArray(builder, stored.HairNames);
            builder.Append(",\"clothing\":");
            AppendClothingArray(builder, stored.ApparelNames, stored.ApparelMaterials);
            builder.Append(",\"hairColors\":");
            AppendPropertyArray(builder, stored.HairColor);
            builder.Append(",\"skinColors\":");
            AppendFilteredPropertyArray(builder, stored.ColorProperties, "skin");
            builder.Append(",\"bodyBlendshapes\":");
            AppendPropertyArray(builder, stored.Blendshapes);
        }
        else
        {
            builder.Append(",\"bodyType\":\"\"");
            builder.Append(",\"hairStyles\":[]");
            builder.Append(",\"clothing\":[]");
            builder.Append(",\"hairColors\":[]");
            builder.Append(",\"skinColors\":[]");
            builder.Append(",\"bodyBlendshapes\":[]");
        }

        builder.Append('}');
    }

    private static void AppendStringArray(StringBuilder builder, List<string> items)
    {
        builder.Append('[');
        if (items != null)
        {
            for (int index = 0; index < items.Count; index++)
            {
                builder.Append('"').Append(Escape(items[index] ?? string.Empty)).Append('"');
                if (index + 1 < items.Count)
                {
                    builder.Append(',');
                }
            }
        }

        builder.Append(']');
    }

    private static void AppendClothingArray(StringBuilder builder, List<string>? apparelNames, List<int>? apparelMaterials)
    {
        builder.Append('[');
        List<string>? nonNullApparelNames = apparelNames;
        int count = nonNullApparelNames != null ? nonNullApparelNames.Count : 0;
        for (int index = 0; index < count; index++)
        {
            builder.Append('{');
            builder.Append("\"slot\":").Append(index);
            builder.Append(",\"name\":\"").Append(Escape(nonNullApparelNames![index] ?? string.Empty)).Append('"');
            int material = apparelMaterials != null && index < apparelMaterials.Count ? apparelMaterials[index] : -1;
            builder.Append(",\"materialIndex\":").Append(material);
            builder.Append('}');
            if (index + 1 < count)
            {
                builder.Append(',');
            }
        }

        builder.Append(']');
    }

    private static void AppendFilteredPropertyArray(StringBuilder builder, List<CC_Property>? properties, string containsText)
    {
        builder.Append('[');
        bool wroteAny = false;
        if (properties != null)
        {
            for (int index = 0; index < properties.Count; index++)
            {
                CC_Property? property = properties[index];
                string propertyName = property?.propertyName ?? string.Empty;
                if (propertyName.IndexOf(containsText, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (wroteAny)
                {
                    builder.Append(',');
                }

                AppendProperty(builder, property);
                wroteAny = true;
            }
        }

        builder.Append(']');
    }

    private static void AppendPropertyArray(StringBuilder builder, List<CC_Property>? properties)
    {
        builder.Append('[');
        if (properties != null)
        {
            for (int index = 0; index < properties.Count; index++)
            {
                AppendProperty(builder, properties[index]);
                if (index + 1 < properties.Count)
                {
                    builder.Append(',');
                }
            }
        }

        builder.Append(']');
    }

    private static void AppendProperty(StringBuilder builder, CC_Property? property)
    {
        builder.Append('{');
        builder.Append("\"propertyName\":\"").Append(Escape(property?.propertyName ?? string.Empty)).Append('"');
        builder.Append(",\"stringValue\":\"").Append(Escape(property?.stringValue ?? string.Empty)).Append('"');
        builder.Append(",\"floatValue\":").Append(property != null ? property.floatValue.ToString("R", CultureInfo.InvariantCulture) : "0");
        builder.Append(",\"materialIndex\":").Append(property != null ? property.materialIndex : -1);
        builder.Append(",\"meshTag\":\"").Append(Escape(property?.meshTag ?? string.Empty)).Append('"');
        builder.Append('}');
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        return builder.ToString().Trim('-');
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

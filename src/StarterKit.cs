using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RagnavikGameplay;

internal readonly struct StarterItem
{
    internal StarterItem(string prefab, int amount)
    {
        Prefab = prefab;
        Amount = amount;
    }

    internal string Prefab { get; }
    internal int Amount { get; }
}

internal static class StarterKit
{
    internal const string Default = "Wood:20,Stone:10,Raspberry:10,CookedMeat:5";

    internal static IReadOnlyList<StarterItem> Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("Starter kit cannot be empty.");
        }

        var items = new List<StarterItem>();
        foreach (var entry in value.Split(','))
        {
            var parts = entry.Split(':');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount < 1)
            {
                throw new FormatException($"Invalid starter item '{entry}'. Use Prefab:Amount.");
            }

            items.Add(new StarterItem(parts[0].Trim(), amount));
        }

        return items;
    }

    internal static string ClaimKey(string platformId, string kitId)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(platformId.Trim() + "\n" + kitId.Trim()));
        return "ragnavik_starter_claim_" + string.Concat(bytes.Take(12).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }
}


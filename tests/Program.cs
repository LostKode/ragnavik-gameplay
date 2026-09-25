using System;
using System.Collections.Generic;
using RagnavikGameplay;

if (args.Length >= 2) ProfessionContracts.Verify(args[0], args[1]);
if (args.Length == 3) ProfessionContracts.VerifyTooltip(args[2]);

var tests = new (string Name, Action Run)[]
{
    ("profession selection changes XP without losing fractions", ProfessionXpRates),
    ("default kit parses", DefaultKitParses),
    ("invalid kit fails", InvalidKitFails),
    ("claim key is stable and kit scoped", ClaimKeyIsStableAndKitScoped),
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

static void DefaultKitParses()
{
    var items = StarterKit.Parse(StarterKit.Default);
    Equal(4, items.Count);
    Equal("Wood", items[0].Prefab);
    Equal(20, items[0].Amount);
}

static void InvalidKitFails()
{
    try
    {
        StarterKit.Parse("Wood:0");
    }
    catch (FormatException)
    {
        return;
    }

    throw new Exception("Expected FormatException.");
}

static void ClaimKeyIsStableAndKitScoped()
{
    var first = StarterKit.ClaimKey("Steam_123", "starter-v1");
    Equal(first, StarterKit.ClaimKey("Steam_123", "starter-v1"));
    NotEqual(first, StarterKit.ClaimKey("Steam_123", "starter-v2"));
    NotEqual(first, StarterKit.ClaimKey("Steam_456", "starter-v1"));
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new Exception($"Expected {expected}, got {actual}.");
    }
}

static void NotEqual<T>(T left, T right)
{
    if (EqualityComparer<T>.Default.Equals(left, right))
    {
        throw new Exception($"Expected different values, got {left}.");
    }
}


static void ProfessionXpRates()
{
    Equal(10f, ProfessionXpPolicy.Scale(10f, true));
    Equal(5f, ProfessionXpPolicy.Scale(10f, false));
    Equal(0.125f, ProfessionXpPolicy.Scale(0.25f, false));
    Equal(0f, ProfessionXpPolicy.Scale(0f, false));
    Equal(7.5f, ProfessionXpPolicy.Scale(5f, false) + ProfessionXpPolicy.Scale(5f, true));
}

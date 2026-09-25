namespace RagnavikGameplay;

internal static class ProfessionXpPolicy
{
    internal static float Scale(float amount, bool selected) => selected ? amount : amount * 0.5f;
}

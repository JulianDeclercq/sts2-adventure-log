namespace CombatLog.CombatLogCode.UI.Rows;

internal static class NameTruncator
{
    public const int DefaultMax = 20;

    public static string Short(string? s, int max = DefaultMax)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s[..(max - 1)] + "\u2026";
    }
}

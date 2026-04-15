using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;

namespace CombatLog.CombatLogCode.UI.Rows;

internal static class DamageSubRow
{
    // Kept in sync with DamageColors for the BBCode-encoded SetBottomText payload.
    private const string HpLostHex = "#E64D4D";
    private const string BlockedHex = "#99B3E6";
    private const string NeutralHex = "#CCCCCC";

    public static NStatEntry Create(
        string victimName, uint? victimCombatId, uint? sourceCombatId,
        int hpLost, int blocked, bool killed,
        CreatureHighlighter highlighter)
    {
        var entry = PreloadManager.Cache.GetScene(NStatEntry.ScenePath)
            .Instantiate<NStatEntry>();

        entry.Ready += () =>
        {
            entry._icon.Visible = false;
            entry._bottomLabel.Visible = false;
            entry.SetTopText($"→ {victimName}: {BuildDamageLine(hpLost, blocked, killed)}");
        };

        entry.MouseEntered += () =>
        {
            highlighter.Highlight(sourceCombatId);
            highlighter.Highlight(victimCombatId);
        };
        entry.MouseExited += () => highlighter.Clear();

        return entry;
    }

    private static string BuildDamageLine(int hpLost, int blocked, bool killed)
    {
        if (hpLost == 0 && blocked == 0 && !killed)
            return $"[color={NeutralHex}]no damage[/color]";

        var parts = new List<string>();
        if (hpLost > 0) parts.Add($"[color={HpLostHex}]-{hpLost} HP[/color]");
        if (blocked > 0)
        {
            var segment = $"[color={BlockedHex}]{blocked} blocked[/color]";
            parts.Add(hpLost > 0 ? $"({segment})" : segment);
        }
        if (killed) parts.Add($"[color={HpLostHex}]💀[/color]");
        return string.Join(" ", parts);
    }
}

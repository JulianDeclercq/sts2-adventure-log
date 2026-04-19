using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Logs card upgrades performed mid-combat (Armaments, Apotheosis, Blessing of
/// the Forge, etc.). Patches CardCmd.Upgrade(IEnumerable...) since the single-card
/// overload delegates to it. Out-of-combat upgrades (smith fountain) are skipped
/// via the IsInProgress guard.
/// </summary>
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Upgrade),
    new[] { typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle) })]
public static class CardUpgradePatch
{
    [HarmonyPostfix]
    public static void Postfix(IEnumerable<CardModel> cards)
    {
        try
        {
            if (cards is null) return;
            if (!CombatManager.Instance?.IsInProgress ?? true) return;

            foreach (var card in cards)
            {
                if (card is null) continue;
                var name = card.Title ?? card.GetType().Name;
                var ownerNetId = card.Owner?.NetId;
                OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

                AdventureLogTracker.RecordCardUpgrade(name, card, ownerNetId, ownerName, isLocal);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording card upgrade: {e.Message}");
        }
    }
}

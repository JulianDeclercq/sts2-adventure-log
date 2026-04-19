using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Logs cards discarded via card/relic effects (Prepared, Dagger Throw, etc.).
/// Only CardCmd.DiscardAndDraw calls CombatHistory.CardDiscarded, so end-of-turn
/// hand discards — which go through CardPileCmd.Add directly — are skipped.
/// </summary>
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardDiscarded))]
public static class CardDiscardPatch
{
    [HarmonyPostfix]
    public static void Postfix(CombatState __0, CardModel __1)
    {
        try
        {
            var card = __1;
            if (card is null) return;

            var name = card.Title ?? card.GetType().Name;
            var ownerNetId = card.Owner?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            AdventureLogTracker.RecordCardDiscard(name, card, ownerNetId, ownerName, isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording card discard: {e.Message}");
        }
    }
}

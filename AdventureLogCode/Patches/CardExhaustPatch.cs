using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Logs cards exhausted via card/relic effects (Strike-class exhaust, Feed,
/// ethereal end-of-turn, Corruption-equivalents). Mirrors CardDiscardPatch.
/// </summary>
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardExhausted))]
public static class CardExhaustPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState __0, CardModel __1)
    {
        try
        {
            var card = __1;
            if (card is null) return;

            var name = card.Title ?? card.GetType().Name;
            var ownerNetId = card.Owner?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            AdventureLogTracker.RecordCardExhaust(name, card, ownerNetId, ownerName, isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording card exhaust: {e.Message}");
        }
    }
}

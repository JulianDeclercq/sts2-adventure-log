using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Logs cards generated mid-combat: Souls from Dirge, Wraith Form spawns,
/// status cards (Wound/Slimed/Burn) added by enemies, potion-added cards
/// (Attack/Cunning/Cosmic potions). The generatedByPlayer flag drives whether
/// the row nests under a card play / potion or renders standalone.
/// </summary>
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardGenerated))]
public static class CardGeneratedPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState __0, CardModel __1, Player? __2)
    {
        try
        {
            var card = __1;
            var generatedByPlayer = __2 is not null;
            if (card is null) return;

            var name = card.Title ?? card.GetType().Name;
            var pile = card.Pile?.Type;
            var ownerNetId = card.Owner?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            AdventureLogTracker.RecordCardGenerated(
                name, card, pile, generatedByPlayer,
                ownerNetId, ownerName, isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording card generated: {e.Message}");
        }
    }
}

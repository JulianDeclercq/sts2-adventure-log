using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Logs status afflictions applied to deck cards (Wound, Slimed, Burn, etc.).
/// Fires whenever an enemy effect or curse afflicts a player's card.
/// </summary>
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardAfflicted))]
public static class CardAfflictionPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState __0, CardModel __1, AfflictionModel __2)
    {
        try
        {
            var card = __1;
            var affliction = __2;
            if (card is null || affliction is null) return;

            var cardName = card.Title ?? card.GetType().Name;
            var afflictionTitle = affliction.Title?.GetFormattedText()
                ?? affliction.Id?.Entry
                ?? affliction.GetType().Name;
            var ownerNetId = card.Owner?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            AdventureLogTracker.RecordCardAffliction(
                cardName, card, afflictionTitle,
                ownerNetId, ownerName, isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording card affliction: {e.Message}");
        }
    }
}

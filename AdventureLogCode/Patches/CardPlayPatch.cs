using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Patches CardModel.OnPlayWrapper to record each card as it's played.
/// OnPlayWrapper signature: (PlayerChoiceContext, Creature? target, bool, ResourceInfo, bool)
/// </summary>
[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
public static class CardPlayPatch
{
    [HarmonyPrefix]
    public static void Prefix(CardModel __instance, Creature? __1)
    {
        try
        {
            var cardName = __instance.Title ?? __instance.GetType().Name;
            var ownerNetId = __instance.Owner?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            var targetName = __1?.Name ?? "";
            var targetCombatId = __1?.CombatId;
            var playerCombatId = __instance.Owner?.Creature?.CombatId;

            AdventureLogTracker.RecordCardPlay(
                cardName, __instance,
                ownerNetId, ownerName, isLocal,
                targetName, targetCombatId, playerCombatId);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording card play: {e.Message}");
        }
    }
}

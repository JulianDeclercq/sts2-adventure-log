using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CombatLog.CombatLogCode.Patches;

/// <summary>
/// Patches CombatHistory.DamageReceived — the game's own per-damage history call —
/// to mirror each damage into our log.
/// Signature: DamageReceived(CombatState, Creature receiver, Creature? dealer, DamageResult result, CardModel? cardSource)
/// Sync, fires only during combat (gated by caller at CreatureCmd.Damage:176).
/// </summary>
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.DamageReceived))]
public static class DamageReceivedPatch
{
    private static readonly HashSet<string> SelfDamagingPowers =
        new(StringComparer.Ordinal) { "Poison", "Doom", "Constrict" };

    private static string? ResolveSelfDamagingPower(Creature receiver)
    {
        foreach (var p in receiver.Powers)
            if (p.Amount > 0 && SelfDamagingPowers.Contains(p.Id?.Entry ?? ""))
                return p.Title?.GetFormattedText() ?? p.Id?.Entry;
        return null;
    }

    [HarmonyPostfix]
    public static void Postfix(CombatState __0, Creature __1, Creature? __2, DamageResult __3, CardModel? __4)
    {
        try
        {
            var receiver = __1;
            var dealer = __2;
            var result = __3;
            var cardSource = __4;

            var ownerNetId = receiver.Player?.NetId ?? dealer?.Player?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            var sourceCardName = cardSource?.Title;
            var sourceName = !string.IsNullOrEmpty(sourceCardName)
                ? sourceCardName!
                : dealer?.Name ?? ResolveSelfDamagingPower(receiver) ?? "";

            CombatLogTracker.RecordDamageReceived(
                victimName: receiver.Name,
                victimCombatId: receiver.CombatId,
                sourceName: sourceName,
                sourceCombatId: dealer?.CombatId,
                sourceCardName: sourceCardName,
                blocked: result.BlockedDamage,
                hpLost: result.UnblockedDamage,
                overkill: result.OverkillDamage,
                wasKilled: result.WasTargetKilled,
                wasFullyBlocked: result.WasFullyBlocked,
                ownerNetId: ownerNetId,
                ownerName: ownerName,
                isLocal: isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error recording damage: {e.Message}");
        }
    }
}

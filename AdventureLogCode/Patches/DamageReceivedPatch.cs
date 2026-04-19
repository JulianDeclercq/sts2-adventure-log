using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Patches the base 6-arg <see cref="CreatureCmd.Damage"/> overload — every other
/// Damage overload delegates here, so this catches all combat damage.
///
/// Why not patch CombatHistory.DamageReceived (the obvious hook)? CreatureCmd.Damage
/// gates that call behind `IsInProgress &amp;&amp; !IsEnding`. The killing hit on the
/// last alive primary enemy flips IsEnding true *before* the gate runs, so the
/// fatal DamageResult is silently dropped from CombatHistory and any patch on it
/// never fires. Patching here records every result including kills.
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
    typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>),
    typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel))]
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

    private static void RecordOne(Creature receiver, Creature? dealer, DamageResult result, CardModel? cardSource)
    {
        var ownerNetId = receiver.Player?.NetId ?? dealer?.Player?.NetId;
        OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

        var sourceCardName = cardSource?.Title;
        var sourceName = !string.IsNullOrEmpty(sourceCardName)
            ? sourceCardName!
            : dealer?.Name ?? ResolveSelfDamagingPower(receiver) ?? "";

        AdventureLogTracker.RecordDamageReceived(
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

    [HarmonyPostfix]
    public static void Postfix(Task<IEnumerable<DamageResult>> __result, Creature? __4, CardModel? __5)
    {
        if (__result is null) return;
        var dealer = __4;
        var cardSource = __5;
        __result.ContinueWith(t =>
        {
            try
            {
                if (!t.IsCompletedSuccessfully || t.Result is null) return;
                foreach (var r in t.Result)
                {
                    if (r is null) continue;
                    RecordOne(r.Receiver, dealer, r, cardSource);
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"[AdventureLog] Error recording damage: {e.Message}");
            }
        }, TaskContinuationOptions.ExecuteSynchronously);
    }
}

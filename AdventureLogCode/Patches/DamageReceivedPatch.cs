using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
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
///
/// The Prefix snapshots the damage modifiers (Strength, Lethality, Vulnerable…) by
/// re-running <see cref="Hook.ModifyDamage"/> per target with the exact same args
/// the game is about to use. ModifyDamage reports the modifier models that
/// contributed a non-identity delta — we resolve their titles so the row can show
/// *why* a hit dealt more than the card's printed value.
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

    private static string? ResolveModifierTitle(AbstractModel m)
    {
        try
        {
            return m switch
            {
                PowerModel p => p.Title?.GetFormattedText() ?? p.Id?.Entry,
                _ => Traverse.Create(m).Property<object>("Title").Value is { } title
                    ? Traverse.Create(title).Method("GetFormattedText").GetValue<string>()
                    : m.Id?.Entry,
            };
        }
        catch
        {
            return m.Id?.Entry;
        }
    }

    private static List<string> SnapshotModifiers(
        Creature target, Creature? dealer, decimal amount, ValueProp props, CardModel? cardSource)
    {
        try
        {
            var combatState = target.CombatState;
            var crewForRun = dealer is null ? new[] { target } : new[] { target, dealer };
            var runState = IRunState.GetFrom(crewForRun);
            if (runState is null) return [];
            Hook.ModifyDamage(runState, combatState, target, dealer, amount, props, cardSource,
                ModifyDamageHookType.All, CardPreviewMode.None, out var modifiers);
            if (modifiers is null) return [];
            List<string> names = [];
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (var m in modifiers)
            {
                var name = ResolveModifierTitle(m);
                if (string.IsNullOrEmpty(name)) continue;
                if (seen.Add(name)) names.Add(name);
            }
            return names;
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error snapshotting damage modifiers: {e.Message}");
            return [];
        }
    }

    private static void RecordOne(
        Creature receiver, Creature? dealer, DamageResult result, CardModel? cardSource,
        IReadOnlyList<string> modifiers)
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
            modifiers: modifiers,
            ownerNetId: ownerNetId,
            ownerName: ownerName,
            isLocal: isLocal);
    }

    [HarmonyPrefix]
    public static void Prefix(
        IEnumerable<Creature> __1, decimal __2, ValueProp __3, Creature? __4, CardModel? __5,
        out Dictionary<uint, List<string>>? __state)
    {
        __state = null;
        try
        {
            var targets = __1?.ToList();
            if (targets is null || targets.Count == 0) return;
            var dict = new Dictionary<uint, List<string>>();
            foreach (var t in targets)
            {
                if (t is null || t.IsDead) continue;
                if (!t.CombatId.HasValue) continue;
                if (dict.ContainsKey(t.CombatId.Value)) continue;
                dict[t.CombatId.Value] = SnapshotModifiers(t, __4, __2, __3, __5);
            }
            __state = dict.Count > 0 ? dict : null;
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error snapshotting damage modifiers prefix: {e.Message}");
        }
    }

    [HarmonyPostfix]
    public static void Postfix(
        Task<IEnumerable<DamageResult>> __result,
        Creature? __4, CardModel? __5,
        Dictionary<uint, List<string>>? __state)
    {
        if (__result is null) return;
        var dealer = __4;
        var cardSource = __5;
        var modifiersByTarget = __state;
        __result.ContinueWith(t =>
        {
            try
            {
                if (!t.IsCompletedSuccessfully || t.Result is null) return;
                foreach (var r in t.Result)
                {
                    if (r is null) continue;
                    var mods = ResolveModifiersFor(r.Receiver, modifiersByTarget);
                    RecordOne(r.Receiver, dealer, r, cardSource, mods);
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"[AdventureLog] Error recording damage: {e.Message}");
            }
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    private static IReadOnlyList<string> ResolveModifiersFor(
        Creature receiver, Dictionary<uint, List<string>>? modifiersByTarget)
    {
        if (modifiersByTarget is null || modifiersByTarget.Count == 0) return [];
        if (receiver.CombatId.HasValue
            && modifiersByTarget.TryGetValue(receiver.CombatId.Value, out var exact))
            return exact;
        // Osty / PetOwner redirection: receiver differs from the original target.
        // With a single original target, the snapshot still applies.
        return modifiersByTarget.Count == 1
            ? modifiersByTarget.Values.First()
            : [];
    }
}

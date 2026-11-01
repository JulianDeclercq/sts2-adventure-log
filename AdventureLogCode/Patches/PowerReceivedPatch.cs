using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Runtime.CompilerServices;

namespace AdventureLog.AdventureLogCode.Patches;

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.PowerReceived))]
public static class PowerReceivedPatch
{
    // Power instance that PowerModel.ApplyInternal just set up. PowerCmd.Apply calls
    // ApplyInternal (sets Owner + Amount) right before History.PowerReceived, so on a
    // first application power.Amount already includes the delta.
    internal static PowerModel? JustAppliedPower;

    // Source card per power instance, written by the PowerCmd.Apply / ModifyAmount
    // prefixes. Keyed by instance so an early-returning Apply or a nested apply during
    // the awaited hooks can't leak its card onto an unrelated power change.
    internal static readonly ConditionalWeakTable<PowerModel, CardModel> SourceCardByPower = new();

    [HarmonyPostfix]
    public static void Postfix(ICombatState __0, PowerModel __1, decimal __2, Creature? __3)
    {
        try
        {
            if ((int)__2 == 0) return;

            var power = __1;
            var applier = __3;

            // decimal is always whole in practice (PowerModel.SetAmount casts to int).
            // First application (PowerCmd.Apply): ApplyInternal already set Amount.
            // Stacking (PowerCmd.ModifyAmount): PowerReceived fires before SetAmount.
            var delta = (int)__2;
            var firstApply = ReferenceEquals(power, JustAppliedPower);
            JustAppliedPower = null;
            var newTotal = firstApply ? power.Amount : power.Amount + delta;

            var powerId = power.Id?.Entry ?? power.GetType().Name;
            var powerTitle = power.Title?.GetFormattedText() ?? powerId;
            var type = power.Type;
            var stackType = power.StackType;
            var icon = power.Icon;

            // Owner is always set by now: ApplyInternal runs first on a new power,
            // and ModifyAmount only targets powers already on a creature.
            var ownerCreature = power.Owner;
            SourceCardByPower.TryGetValue(power, out var sourceCard);
            SourceCardByPower.Remove(power);
            var ownerCreatureName = ownerCreature?.Name ?? "";
            var ownerCreatureCombatId = ownerCreature?.CombatId;

            // Track delayed-energy scheduling so next turn's GainEnergy can be attributed
            // back to the card that applied this power. Generic over any card/relic that
            // applies EnergyNextTurnPower.
            if (power is EnergyNextTurnPower && ownerCreatureCombatId.HasValue)
            {
                var srcName = sourceCard?.Title;
                if (!string.IsNullOrEmpty(srcName))
                    AdventureLogTracker.ScheduledEnergySourceByPlayer[ownerCreatureCombatId.Value] = srcName;
            }

            var applierName = applier?.Name;
            var applierCombatId = applier?.CombatId;

            var ownerNetId = ownerCreature?.Player?.NetId ?? applier?.Player?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            AdventureLogTracker.RecordPowerReceived(
                powerId, powerTitle, type, stackType,
                delta, newTotal,
                ownerCreatureName, ownerCreatureCombatId,
                applierName, applierCombatId,
                icon, power,
                ownerNetId, ownerName, isLocal,
                sourceCard?.Title);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording power received: {e.Message}");
        }
    }
}

[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Apply),
    new[] { typeof(PlayerChoiceContext), typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool) })]
public static class PowerApplySourcePatch
{
    // When the power already exists, Apply forwards to ModifyAmount, whose prefix
    // re-keys the card onto the existing instance.
    [HarmonyPrefix]
    public static void Prefix(PowerModel __1, CardModel? __5) => PowerSource.Set(__1, __5);
}

[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount))]
public static class PowerModifySourcePatch
{
    [HarmonyPrefix]
    public static void Prefix(PowerModel __1, CardModel? __4) => PowerSource.Set(__1, __4);
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ApplyInternal))]
public static class PowerApplyInternalPatch
{
    [HarmonyPrefix]
    public static void Prefix(PowerModel __instance, decimal __1)
    {
        if (__1 != 0m) PowerReceivedPatch.JustAppliedPower = __instance;
    }
}

internal static class PowerSource
{
    public static void Set(PowerModel power, CardModel? card)
    {
        if (card is null) PowerReceivedPatch.SourceCardByPower.Remove(power);
        else PowerReceivedPatch.SourceCardByPower.AddOrUpdate(power, card);
    }
}

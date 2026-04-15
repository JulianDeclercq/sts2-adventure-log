using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CombatLog.CombatLogCode.Patches;

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.PowerReceived))]
public static class PowerReceivedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CombatState __0, PowerModel __1, decimal __2, Creature? __3)
    {
        try
        {
            if ((int)__2 == 0) return;

            var power = __1;
            var applier = __3;

            // decimal is always whole in practice (PowerModel.SetAmount casts to int).
            // PowerReceived fires BEFORE SetAmount mutates (PowerCmd.Apply:525 and
            // ModifyAmount:1393 — both call History.PowerReceived, then SetAmount with the
            // post-apply total). So power.Amount here is pre-apply; compute post-state manually.
            var delta = (int)__2;
            var newTotal = power.Amount + delta;

            var powerId = power.Id?.Entry ?? power.GetType().Name;
            var powerTitle = power.Title?.GetFormattedText() ?? powerId;
            var type = power.Type;
            var stackType = power.StackType;
            var icon = power.Icon;

            var ownerCreature = power.Owner;
            var ownerCreatureName = ownerCreature?.Name ?? "";
            var ownerCreatureCombatId = ownerCreature?.CombatId;

            var applierName = applier?.Name;
            var applierCombatId = applier?.CombatId;

            var ownerNetId = ownerCreature?.Player?.NetId ?? applier?.Player?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            CombatLogTracker.RecordPowerReceived(
                powerId, powerTitle, type, stackType,
                delta, newTotal,
                ownerCreatureName, ownerCreatureCombatId,
                applierName, applierCombatId,
                icon,
                ownerNetId, ownerName, isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error recording power received: {e.Message}");
        }
    }
}

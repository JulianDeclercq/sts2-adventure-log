using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CombatLog.CombatLogCode.Patches;

[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy))]
public static class EnergyDeltaPatch
{
    [HarmonyPostfix]
    public static void Postfix(decimal __0, Player __1)
    {
        try
        {
            var delta = (int)__0;
            if (delta <= 0) return;

            var ownerNetId = __1?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            CombatLogTracker.RecordEnergyDelta(delta, ownerNetId, ownerName, isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error recording energy delta: {e.Message}");
        }
    }
}

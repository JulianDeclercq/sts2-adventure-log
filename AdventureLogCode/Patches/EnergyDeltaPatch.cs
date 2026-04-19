using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AdventureLog.AdventureLogCode.Patches;

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

            var playerCombatId = __1?.Creature?.CombatId;

            // EnergyNextTurnPower.AfterEnergyReset calls GainEnergy BEFORE removing the
            // power, so the power is still on the creature here. If amount matches the
            // delta, this gain came from that power — pop the scheduled source recorded
            // at apply time so the log row can be attributed ("from Invoke").
            string? sourceCardName = null;
            if (playerCombatId.HasValue && __1?.Creature is not null)
            {
                var pendingPower = __1.Creature.GetPower<EnergyNextTurnPower>();
                if (pendingPower is not null && pendingPower.Amount == delta
                    && AdventureLogTracker.ScheduledEnergySourceByPlayer.TryGetValue(playerCombatId.Value, out var srcName))
                {
                    sourceCardName = srcName;
                    AdventureLogTracker.ScheduledEnergySourceByPlayer.Remove(playerCombatId.Value);
                }
            }

            Texture2D? icon = null;
            IHoverTip? hoverTip = null;
            try
            {
                var cardPool = __1?.Character?.CardPool;
                if (cardPool is not null)
                {
                    var path = EnergyIconHelper.GetPath(cardPool);
                    icon = PreloadManager.Cache.GetTexture2D(path);
                }
                if (__1 is not null)
                    hoverTip = HoverTipFactory.ForEnergy(__1);
            }
            catch (Exception iconEx)
            {
                GD.PrintErr($"[AdventureLog] Error loading energy icon/tooltip: {iconEx.Message}");
            }

            AdventureLogTracker.RecordEnergyDelta(
                delta, icon, hoverTip, playerCombatId,
                ownerNetId, ownerName, isLocal,
                sourceCardName);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording energy delta: {e.Message}");
        }
    }
}

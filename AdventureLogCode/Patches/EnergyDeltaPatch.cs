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
    // Energy before the call; -1 when the player has no combat state.
    [HarmonyPrefix]
    public static void Prefix(Player __1, out int __state)
    {
        __state = __1?.PlayerCombatState?.Energy ?? -1;
    }

    [HarmonyPostfix]
    public static void Postfix(ref Task __result, decimal __0, Player __1, int __state)
    {
        try
        {
            var requested = (int)__0;
            if (requested <= 0 || __1 is null) return;

            var playerCombatId = __1.Creature?.CombatId;

            // EnergyNextTurnPower.AfterEnergyReset calls GainEnergy BEFORE removing the
            // power, so the power is still on the creature here (synchronous part of the
            // call). If its amount matches the request, this gain came from that power —
            // pop the scheduled source recorded at apply time ("from Invoke").
            string? sourceCardName = null;
            if (playerCombatId.HasValue && __1.Creature is not null)
            {
                var pendingPower = __1.Creature.GetPower<EnergyNextTurnPower>();
                if (pendingPower is not null && pendingPower.Amount == requested
                    && AdventureLogTracker.ScheduledEnergySourceByPlayer.TryGetValue(playerCombatId.Value, out var srcName))
                {
                    sourceCardName = srcName;
                    AdventureLogTracker.ScheduledEnergySourceByPlayer.Remove(playerCombatId.Value);
                }
            }

            __result = RecordWhenDone(__result, __1, __state, requested, playerCombatId, sourceCardName);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording energy delta: {e.Message}");
        }
    }

    // GainEnergy applies energy modifiers and can skip the gain entirely (combat ending,
    // modified to 0), so log the energy the player actually received once it finishes.
    private static async Task RecordWhenDone(
        Task original, Player player, int before, int requested,
        uint? playerCombatId, string? sourceCardName)
    {
        await original;
        try
        {
            var after = player.PlayerCombatState?.Energy;
            var delta = before >= 0 && after.HasValue ? after.Value - before : requested;
            if (delta <= 0) return;

            OwnerResolver.Resolve(player.NetId, out var ownerName, out var isLocal);

            Texture2D? icon = null;
            IHoverTip? hoverTip = null;
            try
            {
                var cardPool = player.Character?.CardPool;
                if (cardPool is not null)
                {
                    var path = EnergyIconHelper.GetPath(cardPool);
                    icon = PreloadManager.Cache.GetTexture2D(path);
                }
                hoverTip = HoverTipFactory.ForEnergy(player);
            }
            catch (Exception iconEx)
            {
                GD.PrintErr($"[AdventureLog] Error loading energy icon/tooltip: {iconEx.Message}");
            }

            AdventureLogTracker.RecordEnergyDelta(
                delta, icon, hoverTip, playerCombatId,
                player.NetId, ownerName, isLocal,
                sourceCardName);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording energy delta: {e.Message}");
        }
    }
}

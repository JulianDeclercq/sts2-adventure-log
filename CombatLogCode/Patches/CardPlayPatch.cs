using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace CombatLog.CombatLogCode.Patches;

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
            var playerName = "";
            try
            {
                if (__instance.Owner is not null)
                {
                    var name = PlatformUtil.GetPlayerName(
                        RunManager.Instance.NetService.Platform,
                        __instance.Owner.NetId);
                    if (!string.IsNullOrEmpty(name) && !ulong.TryParse(name, out _))
                        playerName = name;
                }
            }
            catch { }

            var targetName = __1?.Name ?? "";
            var targetCombatId = __1?.CombatId;
            var playerCombatId = __instance.Owner?.Creature?.CombatId;

            // MP DIAGNOSTIC: log owner NetId + local player ID to detect whether
            // OnPlayWrapper fires for remote players' cards or only local plays.
            try
            {
                var platform = RunManager.Instance.NetService.Platform;
                ulong localId = PlatformUtil.GetLocalPlayerId(platform);
                ulong ownerId = __instance.Owner?.NetId ?? 0UL;
                bool isLocal = ownerId == localId;
                GD.Print($"[CombatLog MP] card={cardName} owner={ownerId} ({playerName}) local={localId} isLocal={isLocal}");
            }
            catch (Exception diagEx)
            {
                GD.PrintErr($"[CombatLog MP] diag failed: {diagEx.Message}");
            }

            CombatLogTracker.RecordPlay(cardName, __instance, playerName, targetName, targetCombatId, playerCombatId);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error recording card play: {e.Message}");
        }
    }
}

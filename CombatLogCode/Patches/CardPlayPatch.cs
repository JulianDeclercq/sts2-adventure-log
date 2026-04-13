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

            CombatLogTracker.RecordPlay(cardName, __instance, playerName, targetName, targetCombatId);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error recording card play: {e.Message}");
        }
    }
}

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace CombatLog.CombatLogCode.Patches;

/// <summary>
/// Patches CardModel.OnPlayWrapper to record each card as it's played.
/// Uses CardModel.Title for the localized display name.
/// </summary>
[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
public static class CardPlayPatch
{
    [HarmonyPrefix]
    public static void Prefix(CardModel __instance)
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
                    // Fallback returns the NetId as digits — skip it (single player)
                    if (!string.IsNullOrEmpty(name) && !ulong.TryParse(name, out _))
                        playerName = name;
                }
            }
            catch { }
            CombatLogTracker.RecordPlay(cardName, __instance, playerName);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error recording card play: {e.Message}");
        }
    }
}

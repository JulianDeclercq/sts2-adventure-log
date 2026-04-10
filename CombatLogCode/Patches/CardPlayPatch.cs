using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

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
            CombatLogTracker.RecordPlay(cardName);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error recording card play: {e.Message}");
        }
    }
}

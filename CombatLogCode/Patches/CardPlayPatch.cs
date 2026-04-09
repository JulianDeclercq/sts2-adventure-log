using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace CombatLog.CombatLogCode.Patches;

/// <summary>
/// Patches CardModel.OnPlayWrapper to record each card as it's played.
/// Each card type is its own class (e.g., MegaCrit.Sts2.Core.Models.Cards.Bash),
/// so we use the class name as the display name.
/// </summary>
[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
public static class CardPlayPatch
{
    [HarmonyPrefix]
    public static void Prefix(CardModel __instance)
    {
        try
        {
            // Each card is a concrete class like "Bash", "Strike", etc.
            var cardName = __instance.GetType().Name;

            // Try to get a better display name via Id.Entry if available
            try
            {
                var idProp = __instance.GetType().GetProperty("Id");
                if (idProp != null)
                {
                    var id = idProp.GetValue(__instance);
                    if (id != null)
                    {
                        var entryProp = id.GetType().GetProperty("Entry");
                        if (entryProp != null)
                        {
                            var entry = entryProp.GetValue(id)?.ToString();
                            if (!string.IsNullOrEmpty(entry))
                                cardName = entry;
                        }
                    }
                }
            }
            catch
            {
                // Fall back to class name - already set
            }

            CombatLogTracker.RecordPlay(cardName);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error recording card play: {e.Message}");
        }
    }
}

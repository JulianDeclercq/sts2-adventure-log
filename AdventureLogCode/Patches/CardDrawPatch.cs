using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Logs cards drawn via card/relic/power effects (Soul, Skim, Fiddle, etc.). Routine
/// start-of-turn hand draws (fromHandDraw: true) are skipped.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Draw),
    new[] { typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool) })]
public static class CardDrawPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Task<IEnumerable<CardModel>> __result, Player player, bool fromHandDraw)
    {
        if (fromHandDraw) return;
        var original = __result;
        __result = Continuation(original, player);
    }

    private static async Task<IEnumerable<CardModel>> Continuation(
        Task<IEnumerable<CardModel>> original, Player player)
    {
        var drawn = await original;
        try
        {
            if (player is null) return drawn;
            var ownerNetId = player.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            foreach (var card in drawn)
            {
                if (card is null) continue;
                var name = card.Title ?? card.GetType().Name;
                AdventureLogTracker.RecordCardDraw(name, card, ownerNetId, ownerName, isLocal);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording card draw: {e.Message}");
        }
        return drawn;
    }
}

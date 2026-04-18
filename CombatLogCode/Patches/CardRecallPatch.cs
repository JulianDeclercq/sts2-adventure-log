using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CombatLog.CombatLogCode.Patches;

/// <summary>
/// Logs cards moved Discard -> Hand (Dredge, All For One, etc.) so the log shows which card was
/// brought back by the parent play.
/// </summary>
[HarmonyPatch]
public static class CardRecallPatch
{
    private static readonly ConditionalWeakTable<CardModel, PileTypeBox> PreMovePile = new();

    private sealed class PileTypeBox { public PileType Type; }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
        new[] { typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
    public static class SingleByPileType
    {
        [HarmonyPrefix]
        public static void Prefix(CardModel card) => CapturePre(card);

        [HarmonyPostfix]
        public static void Postfix(CardModel card) => EmitIfRecalled(card);
    }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
        new[] { typeof(IEnumerable<CardModel>), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
    public static class ManyByPileType
    {
        [HarmonyPrefix]
        public static void Prefix(IEnumerable<CardModel> cards)
        {
            if (cards is null) return;
            foreach (var c in cards) CapturePre(c);
        }

        [HarmonyPostfix]
        public static void Postfix(IEnumerable<CardModel> cards)
        {
            if (cards is null) return;
            foreach (var c in cards) EmitIfRecalled(c);
        }
    }

    private static void CapturePre(CardModel? card)
    {
        if (card?.Pile is null) return;
        if (PreMovePile.TryGetValue(card, out var box)) box.Type = card.Pile.Type;
        else PreMovePile.Add(card, new PileTypeBox { Type = card.Pile.Type });
    }

    private static void EmitIfRecalled(CardModel? card)
    {
        try
        {
            if (card is null) return;
            if (!PreMovePile.TryGetValue(card, out var box)) return;
            PreMovePile.Remove(card);

            if (box.Type != PileType.Discard) return;
            if (card.Pile?.Type != PileType.Hand) return;

            var recalledName = card.Title ?? card.GetType().Name;
            var ownerNetId = card.Owner?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            CombatLogTracker.RecordCardRecall(recalledName, card, ownerNetId, ownerName, isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error recording card recall: {e.Message}");
        }
    }
}

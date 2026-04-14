using MegaCrit.Sts2.Core.Models;

namespace CombatLog.CombatLogCode;

public static class CombatLogTracker
{
    public record CardPlayEntry(
        string CardName, CardModel? Card, int TurnNumber, int OrderInTurn,
        int CombatNumber, string PlayerName, string TargetName,
        uint? TargetCombatId, uint? PlayerCombatId);

    public static List<CardPlayEntry> History { get; } = new();
    public static int CurrentTurn { get; set; } = 1;
    public static int CurrentCombat { get; set; } = 0;

    private static int _orderCounter;

    public static void RecordPlay(string cardName, CardModel? card, string playerName = "",
        string targetName = "", uint? targetCombatId = null, uint? playerCombatId = null)
    {
        _orderCounter++;
        History.Add(new CardPlayEntry(cardName, card, CurrentTurn, _orderCounter,
            CurrentCombat, playerName, targetName, targetCombatId, playerCombatId));
        OnHistoryChanged?.Invoke();
    }

    public static void OnNewTurn()
    {
        CurrentTurn++;
        _orderCounter = 0;
    }

    public static void OnCombatStart()
    {
        CurrentCombat++;
        CurrentTurn = 0;
        _orderCounter = 0;
    }

    public static void Clear()
    {
        History.Clear();
        CurrentTurn = 1;
        CurrentCombat = 0;
        _orderCounter = 0;
    }

    public static event Action? OnHistoryChanged;
}

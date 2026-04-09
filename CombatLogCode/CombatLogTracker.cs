namespace CombatLog.CombatLogCode;

public static class CombatLogTracker
{
    public record CardPlayEntry(string CardName, int TurnNumber, int OrderInTurn, int CombatNumber);

    public static List<CardPlayEntry> History { get; } = new();
    public static int CurrentTurn { get; set; } = 1;
    public static int CurrentCombat { get; set; } = 0;

    private static int _orderCounter;

    public static void RecordPlay(string cardName)
    {
        _orderCounter++;
        History.Add(new CardPlayEntry(cardName, CurrentTurn, _orderCounter, CurrentCombat));
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
        CurrentTurn = 1;
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

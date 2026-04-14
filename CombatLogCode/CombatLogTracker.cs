using CombatLog.CombatLogCode.Events;
using MegaCrit.Sts2.Core.Models;

namespace CombatLog.CombatLogCode;

public static class CombatLogTracker
{
    public static List<LogEvent> History { get; } = new();
    public static int CurrentTurn { get; set; } = 1;
    public static int CurrentCombat { get; set; } = 0;

    private static int _orderCounter;

    public static void RecordCardPlay(
        string cardName, CardModel? card,
        ulong? ownerNetId, string ownerName, bool isLocal,
        string targetName = "", uint? targetCombatId = null, uint? playerCombatId = null)
    {
        _orderCounter++;
        var e = new CardPlayEvent(
            cardName, card, targetName, targetCombatId, playerCombatId,
            ownerNetId, ownerName, isLocal,
            CurrentTurn, _orderCounter, CurrentCombat);
        Append(e);
    }

    public static void RecordDamageReceived(
        string victimName, uint? victimCombatId,
        string sourceName, uint? sourceCombatId, string? sourceCardName,
        int blocked, int hpLost, int overkill, bool wasKilled, bool wasFullyBlocked,
        ulong? ownerNetId, string ownerName, bool isLocal)
    {
        _orderCounter++;
        var e = new DamageReceivedEvent(
            victimName, victimCombatId,
            sourceName, sourceCombatId, sourceCardName,
            blocked, hpLost, overkill, wasKilled, wasFullyBlocked,
            ownerNetId, ownerName, isLocal,
            CurrentTurn, _orderCounter, CurrentCombat);
        Append(e);
    }

    public static void RecordRelicProc(
        string relicName, string relicId, RelicModel? relic,
        IReadOnlyList<string> targetNames, IReadOnlyList<uint?> targetCombatIds,
        ulong? ownerNetId, string ownerName, bool isLocal)
    {
        _orderCounter++;
        var e = new RelicProcEvent(
            relicName, relicId, relic,
            targetNames, targetCombatIds,
            ownerNetId, ownerName, isLocal,
            CurrentTurn, _orderCounter, CurrentCombat);
        Append(e);
    }

    public static void Append(LogEvent e)
    {
        History.Add(e);
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

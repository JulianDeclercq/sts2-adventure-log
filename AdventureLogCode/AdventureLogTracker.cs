using AdventureLog.AdventureLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode;

public static class AdventureLogTracker
{
    public static List<LogEvent> History { get; } = new();
    public static int CurrentTurn { get; private set; } = 1;
    public static int CurrentCombat { get; private set; } = 0;

    private static int _orderCounter;
    private static bool _firstPlayerTurn;

    // Source card name for a pending "next turn" energy gain, keyed by player CombatId.
    // Populated when EnergyNextTurnPower is applied (PowerReceivedPatch). Consumed next
    // turn when PlayerCmd.GainEnergy fires from EnergyNextTurnPower.AfterEnergyReset.
    internal static readonly Dictionary<uint, string> ScheduledEnergySourceByPlayer = new();

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

    public static void RecordEnergyDelta(
        int delta, Texture2D? icon, IHoverTip? hoverTip, uint? playerCombatId,
        ulong? ownerNetId, string ownerName, bool isLocal,
        string? sourceCardName = null)
    {
        _orderCounter++;
        var e = new EnergyDeltaEvent(
            delta, icon, hoverTip, playerCombatId,
            ownerNetId, ownerName, isLocal,
            CurrentTurn, _orderCounter, CurrentCombat,
            sourceCardName);
        Append(e);
    }

    public static void RecordCardRecall(
        string recalledCardName, CardModel? recalledCard,
        ulong? ownerNetId, string ownerName, bool isLocal)
    {
        _orderCounter++;
        var e = new CardRecallEvent(
            recalledCardName, recalledCard,
            ownerNetId, ownerName, isLocal,
            CurrentTurn, _orderCounter, CurrentCombat);
        Append(e);
    }

    public static void RecordCardDraw(
        string drawnCardName, CardModel? drawnCard,
        ulong? ownerNetId, string ownerName, bool isLocal)
    {
        _orderCounter++;
        var e = new CardDrawEvent(
            drawnCardName, drawnCard,
            ownerNetId, ownerName, isLocal,
            CurrentTurn, _orderCounter, CurrentCombat);
        Append(e);
    }

    public static void RecordCardDiscard(
        string discardedCardName, CardModel? discardedCard,
        ulong? ownerNetId, string ownerName, bool isLocal)
    {
        _orderCounter++;
        var e = new CardDiscardEvent(
            discardedCardName, discardedCard,
            ownerNetId, ownerName, isLocal,
            CurrentTurn, _orderCounter, CurrentCombat);
        Append(e);
    }

    public static void RecordPowerReceived(
        string powerId, string powerTitle, PowerType type, PowerStackType stackType,
        int delta, int newTotal,
        string ownerCreatureName, uint? ownerCreatureCombatId,
        string? applierName, uint? applierCombatId,
        Texture2D? icon, PowerModel? power,
        ulong? ownerNetId, string ownerName, bool isLocal)
    {
        _orderCounter++;
        var e = new PowerReceivedEvent(
            powerId, powerTitle, type, stackType, delta, newTotal,
            ownerCreatureName, ownerCreatureCombatId,
            applierName, applierCombatId, icon, power,
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
        // First SetupPlayerTurn of a combat keeps Turn 0 so start-of-combat relic procs
        // and first-turn procs land in the same bucket.
        if (_firstPlayerTurn) _firstPlayerTurn = false;
        else CurrentTurn++;
        _orderCounter = 0;
    }

    public static void OnCombatStart()
    {
        CurrentCombat++;
        CurrentTurn = 0;
        _orderCounter = 0;
        _firstPlayerTurn = true;
        History.Clear();
        ScheduledEnergySourceByPlayer.Clear();
        OnHistoryChanged?.Invoke();
    }

    public static event Action? OnHistoryChanged;
}

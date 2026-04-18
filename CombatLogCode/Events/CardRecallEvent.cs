using MegaCrit.Sts2.Core.Models;

namespace CombatLog.CombatLogCode.Events;

public sealed record CardRecallEvent(
    string RecalledCardName,
    CardModel? RecalledCard,
    ulong? OwnerNetId,
    string OwnerName,
    bool IsLocal,
    int TurnNumber,
    int OrderInTurn,
    int CombatNumber)
    : LogEvent(OwnerNetId, OwnerName, IsLocal, TurnNumber, OrderInTurn, CombatNumber);

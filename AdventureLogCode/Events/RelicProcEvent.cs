using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode.Events;

public sealed record RelicProcEvent(
    string RelicName,
    string RelicId,
    RelicModel? Relic,
    IReadOnlyList<string> TargetNames,
    IReadOnlyList<uint?> TargetCombatIds,
    ulong? OwnerNetId,
    string OwnerName,
    bool IsLocal,
    int TurnNumber,
    int OrderInTurn,
    int CombatNumber)
    : LogEvent(OwnerNetId, OwnerName, IsLocal, TurnNumber, OrderInTurn, CombatNumber);

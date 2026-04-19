namespace AdventureLog.AdventureLogCode.Events;

public abstract record LogEvent(
    ulong? OwnerNetId,
    string OwnerName,
    bool IsLocal,
    int TurnNumber,
    int OrderInTurn,
    int CombatNumber);

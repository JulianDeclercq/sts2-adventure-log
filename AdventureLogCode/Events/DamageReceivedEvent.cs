namespace AdventureLog.AdventureLogCode.Events;

public sealed record DamageReceivedEvent(
    string VictimName,
    uint? VictimCombatId,
    string SourceName,
    uint? SourceCombatId,
    string? SourceCardName,
    int BlockedDamage,
    int HpLost,
    int OverkillDamage,
    bool WasKilled,
    bool WasFullyBlocked,
    IReadOnlyList<string> Modifiers,
    ulong? OwnerNetId,
    string OwnerName,
    bool IsLocal,
    int TurnNumber,
    int OrderInTurn,
    int CombatNumber)
    : LogEvent(OwnerNetId, OwnerName, IsLocal, TurnNumber, OrderInTurn, CombatNumber)
{
    public int RawDamage => BlockedDamage + HpLost;
}

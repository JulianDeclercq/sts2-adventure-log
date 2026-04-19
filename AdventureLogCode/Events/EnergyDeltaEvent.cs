using Godot;
using MegaCrit.Sts2.Core.HoverTips;

namespace AdventureLog.AdventureLogCode.Events;

public sealed record EnergyDeltaEvent(
    int Delta,
    Texture2D? Icon,
    IHoverTip? HoverTip,
    uint? PlayerCombatId,
    ulong? OwnerNetId,
    string OwnerName,
    bool IsLocal,
    int TurnNumber,
    int OrderInTurn,
    int CombatNumber,
    string? SourceCardName = null)
    : LogEvent(OwnerNetId, OwnerName, IsLocal, TurnNumber, OrderInTurn, CombatNumber);

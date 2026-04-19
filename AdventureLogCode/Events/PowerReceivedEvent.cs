using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode.Events;

public sealed record PowerReceivedEvent(
    string PowerId,
    string PowerTitle,
    PowerType Type,
    PowerStackType StackType,
    int Delta,
    int NewTotal,
    string OwnerCreatureName,
    uint? OwnerCreatureCombatId,
    string? ApplierName,
    uint? ApplierCombatId,
    Texture2D? Icon,
    PowerModel? Power,
    ulong? OwnerNetId,
    string OwnerName,
    bool IsLocal,
    int TurnNumber,
    int OrderInTurn,
    int CombatNumber)
    : LogEvent(OwnerNetId, OwnerName, IsLocal, TurnNumber, OrderInTurn, CombatNumber);

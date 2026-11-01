using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Patches CombatManager.StartTurn + SetupPlayerTurn to track turn progression (player turns only),
/// and CombatRoom.StartCombat to reset and clear the log at combat entry. The latter
/// must fire before Hook.AfterRoomEntered, where start-of-combat relics Flash.
/// </summary>
// StartTurn runs once per side turn; SetupPlayerTurn then runs once per player in a loop.
// Only the first SetupPlayerTurn after a StartTurn counts, so multiplayer advances one
// turn per round (and extra turns still count as a new turn).
[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.StartTurn))]
public static class TurnBeginPatch
{
    [HarmonyPrefix]
    public static void Prefix() => AdventureLogTracker.TurnPending = true;
}

[HarmonyPatch(typeof(CombatManager), "SetupPlayerTurn")]
public static class TurnStartPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        if (!AdventureLogTracker.TurnPending) return;
        AdventureLogTracker.TurnPending = false;
        AdventureLogTracker.OnNewTurn();
    }
}

[HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.StartCombat))]
public static class CombatStartPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        AdventureLogTracker.OnCombatStart();
    }
}

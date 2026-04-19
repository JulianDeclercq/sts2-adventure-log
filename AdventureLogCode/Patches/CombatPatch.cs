using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Patches CombatManager.SetupPlayerTurn to track turn progression (player turns only),
/// and CombatRoom.StartCombat to reset and clear the log at combat entry. The latter
/// must fire before Hook.AfterRoomEntered, where start-of-combat relics Flash.
/// </summary>
[HarmonyPatch(typeof(CombatManager), "SetupPlayerTurn")]
public static class TurnStartPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
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

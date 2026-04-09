using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;

namespace CombatLog.CombatLogCode.Patches;

/// <summary>
/// Patches CombatManager.StartTurn to track turn progression,
/// and StartCombatInternal to reset turn counter per combat.
/// </summary>
[HarmonyPatch(typeof(CombatManager), "StartTurn")]
public static class TurnStartPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        CombatLogTracker.OnNewTurn();
    }
}

[HarmonyPatch(typeof(CombatManager), "StartCombatInternal")]
public static class CombatStartPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        CombatLogTracker.OnCombatStart();
    }
}

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace CombatLog.CombatLogCode.Patches;

/// <summary>
/// Adds a -fastcontinue launch flag that auto-clicks the main menu Continue button.
/// Mirrors how the game's built-in -fastmp auto-enters multiplayer flows.
/// </summary>
[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu.CheckCommandLineArgs))]
public static class FastContinuePatch
{
    [HarmonyPostfix]
    public static void Postfix(NMainMenu __instance)
    {
        try
        {
            if (!CommandLineHelper.HasArg("fastcontinue")) return;
            __instance.OnContinueButtonPressed(null!);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] fastcontinue failed: {e.Message}");
        }
    }
}

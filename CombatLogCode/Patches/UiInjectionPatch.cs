using CombatLog.CombatLogCode.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CombatLog.CombatLogCode.Patches;

/// <summary>
/// Injects the CombatLogPanel into the scene tree when a combat room node is ready.
/// NCombatRoom is the Godot Node version of the combat room.
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class UiInjectionPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatRoom __instance)
    {
        try
        {
            if (CombatLogPanel.Instance != null) return;

            var panel = new CombatLogPanel();
            panel.Name = "CombatLogPanel";

            var root = __instance.GetTree()?.Root;
            root?.CallDeferred(Node.MethodName.AddChild, panel);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error injecting combat log panel: {e.Message}");
        }
    }
}

using CombatLog.CombatLogCode.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CombatLog.CombatLogCode.Patches;

/// <summary>
/// Injects the CombatLogPanel and HistoryButton into the scene tree when a combat room node is ready.
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

        try
        {
            var discardBtn = __instance
                .FindChildren("*", recursive: true, owned: false)
                .OfType<NDiscardPileButton>()
                .FirstOrDefault();

            if (discardBtn is null)
            {
                GD.PrintErr("[CombatLog] NDiscardPileButton not found.");
                return;
            }

            var container = discardBtn.GetParent();

            var btn = new HistoryButton();
            btn.Name = "CombatLogHistoryButton";
            btn.Size = discardBtn.Size;
            btn.Position = new Vector2(
                discardBtn.Position.X - discardBtn.Size.X - 18,
                discardBtn.Position.Y
            );

            container.CallDeferred(Node.MethodName.AddChild, btn);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Error injecting history button: {e.Message}");
        }
    }
}

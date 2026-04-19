using AdventureLog.AdventureLogCode.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Injects the AdventureLogPanel under NRun so it lives for the whole run (map + combat + events)
/// and is destroyed automatically when the run ends / returns to the main menu.
/// </summary>
[HarmonyPatch(typeof(NRun), "_Ready")]
public static class PanelInjectionPatch
{
    [HarmonyPostfix]
    public static void Postfix(NRun __instance)
    {
        try
        {
            if (AdventureLogPanel.Instance != null) return;

            var panel = new AdventureLogPanel();
            panel.Name = "AdventureLogPanel";
            __instance.CallDeferred(Node.MethodName.AddChild, panel);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error injecting combat log panel: {e.Message}");
        }
    }
}

/// <summary>
/// Injects the HistoryButton next to the discard pile button when a combat room is ready.
/// Combat-only UI; the panel itself already exists (injected at run start).
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class HistoryButtonInjectionPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatRoom __instance)
    {
        try
        {
            var discardBtn = __instance
                .FindChildren("*", recursive: true, owned: false)
                .OfType<NDiscardPileButton>()
                .FirstOrDefault();

            if (discardBtn is null)
            {
                GD.PrintErr("[AdventureLog] NDiscardPileButton not found.");
                return;
            }

            var container = discardBtn.GetParent();

            var btn = new HistoryButton();
            btn.Name = "AdventureLogHistoryButton";
            btn.Size = discardBtn.Size;
            btn.Position = new Vector2(
                discardBtn.Position.X - discardBtn.Size.X - 18,
                discardBtn.Position.Y
            );

            container.CallDeferred(Node.MethodName.AddChild, btn);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error injecting history button: {e.Message}");
        }
    }
}

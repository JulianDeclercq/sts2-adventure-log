using Godot;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace AdventureLog.AdventureLogCode.UI;

public static class HoverTipHelper
{
    public static void PositionLeftOfCursor(Control owner, NHoverTipSet tip)
    {
        tip.ZIndex = 4096;

        void Reposition()
        {
            if (!GodotObject.IsInstanceValid(tip)) return;
            var mouse = owner.GetGlobalMousePosition();
            tip.GlobalPosition = mouse + new Vector2(-tip.Size.X - 20, 20);
        }

        Reposition();
        tip.Resized += Reposition;
    }
}

using CombatLog.CombatLogCode.Events;
using Godot;

namespace CombatLog.CombatLogCode.UI.Rows;

public partial class EnergySubRow : HBoxContainer
{
    private static readonly Color EnergyColor = new(0.4f, 0.85f, 1.0f);

    private readonly EnergyDeltaEvent _entry;

    public EnergySubRow(EnergyDeltaEvent entry)
    {
        _entry = entry;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;

        AddChild(new Label { Text = "    " });

        var label = new Label();
        label.Text = $"+{_entry.Delta} energy";
        label.AddThemeColorOverride("font_color", EnergyColor);
        AddChild(label);
    }
}

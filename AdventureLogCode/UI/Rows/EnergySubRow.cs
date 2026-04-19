using AdventureLog.AdventureLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace AdventureLog.AdventureLogCode.UI.Rows;

public partial class EnergySubRow : HBoxContainer
{
    private static readonly Color EnergyColor = new(0.4f, 0.85f, 1.0f);
    private static readonly Color HoverColor = new(1.0f, 1.0f, 0.6f);
    private const float IconSize = 16;

    private readonly EnergyDeltaEvent _entry;
    private readonly CreatureHighlighter _highlighter;

    public EnergySubRow(EnergyDeltaEvent entry, CreatureHighlighter highlighter)
    {
        _entry = entry;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

        AddChild(new Label { Text = "    " });

        if (_entry.Icon is not null)
        {
            AddChild(new TextureRect
            {
                Texture = _entry.Icon,
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            });
        }

        var label = new Label();
        label.Text = $"+{_entry.Delta} energy";
        label.AddThemeColorOverride("font_color", EnergyColor);
        AddChild(label);

        MouseEntered += () =>
        {
            label.AddThemeColorOverride("font_color", HoverColor);
            _highlighter.Highlight(_entry.PlayerCombatId);
            if (_entry.HoverTip is not null)
                try
                {
                    var tip = NHoverTipSet.CreateAndShow(this, _entry.HoverTip);
                    if (tip is not null) HoverTipHelper.PositionLeftOfCursor(this, tip);
                }
                catch { }
        };

        MouseExited += () =>
        {
            label.AddThemeColorOverride("font_color", EnergyColor);
            _highlighter.Clear();
            try { NHoverTipSet.Remove(this); } catch { }
        };

        TreeExiting += () =>
        {
            try { NHoverTipSet.Remove(this); } catch { }
        };
    }
}

using Godot;

namespace AdventureLog.AdventureLogCode.UI.Rows;

public partial class SourceHeaderRow : HBoxContainer
{
    private readonly string _sourceName;
    private readonly uint? _combatId;
    private readonly CreatureHighlighter _highlighter;

    public SourceHeaderRow(string sourceName, uint? combatId, CreatureHighlighter highlighter)
    {
        _sourceName = sourceName;
        _combatId = combatId;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

        var label = new Label();
        label.Text = $"{NameTruncator.Short(_sourceName)}:";
        label.AddThemeColorOverride("font_color", DamageColors.Source);
        AddChild(label);

        MouseEntered += () =>
        {
            label.AddThemeColorOverride("font_color", DamageColors.Hover);
            _highlighter.Highlight(_combatId);
        };

        MouseExited += () =>
        {
            label.AddThemeColorOverride("font_color", DamageColors.Source);
            _highlighter.Clear();
        };
    }
}

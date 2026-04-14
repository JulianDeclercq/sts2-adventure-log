using CombatLog.CombatLogCode.Events;
using Godot;

namespace CombatLog.CombatLogCode.UI.Rows;

public partial class RelicEntryRow : HBoxContainer
{
    private static readonly Color RelicColor = new(1.0f, 0.85f, 0.4f);
    private static readonly Color TargetColor = new(0.7f, 0.6f, 0.5f);
    private static readonly Color HoverColor = new(1.0f, 0.95f, 0.5f);
    private const float IconSize = 20;

    private readonly RelicProcEvent _entry;
    private readonly CreatureHighlighter _highlighter;

    public RelicEntryRow(RelicProcEvent entry, CreatureHighlighter highlighter)
    {
        _entry = entry;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;

        if (_entry.Relic is not null)
        {
            var tex = _entry.Relic.Icon;
            if (tex is not null)
            {
                var rect = new TextureRect
                {
                    Texture = tex,
                    CustomMinimumSize = new Vector2(IconSize, IconSize),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                };
                AddChild(rect);
            }
        }

        var labels = new List<Label>();

        var nameText = string.IsNullOrEmpty(_entry.OwnerName)
            ? _entry.RelicName
            : $"{_entry.RelicName} [{_entry.OwnerName}]";
        labels.Add(AppendLabel(nameText, RelicColor));

        if (_entry.TargetNames.Count > 0)
        {
            var targets = string.Join(", ", _entry.TargetNames);
            labels.Add(AppendLabel($"→ {targets}", TargetColor));
        }

        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();
        var targetIds = _entry.TargetCombatIds;

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", HoverColor);
            foreach (var id in targetIds) _highlighter.Highlight(id);
        };

        MouseExited += () =>
        {
            for (int i = 0; i < labels.Count; i++)
                labels[i].AddThemeColorOverride("font_color", originalColors[i]);
            _highlighter.Clear();
        };
    }

    private Label AppendLabel(string text, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color);
        AddChild(label);
        return label;
    }
}

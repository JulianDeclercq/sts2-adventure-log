using CombatLog.CombatLogCode.Events;
using Godot;

namespace CombatLog.CombatLogCode.UI.Rows;

public partial class DamageEntryRow : HBoxContainer
{
    private static readonly Color HpLostColor = new(0.9f, 0.3f, 0.3f);
    private static readonly Color BlockedColor = new(0.6f, 0.7f, 0.9f);
    private static readonly Color NeutralColor = new(0.8f, 0.8f, 0.8f);
    private static readonly Color SourceColor = new(0.75f, 0.65f, 0.55f);
    private static readonly Color HoverColor = new(1.0f, 0.95f, 0.5f);

    private readonly DamageReceivedEvent _entry;
    private readonly CreatureHighlighter _highlighter;

    public DamageEntryRow(DamageReceivedEvent entry, CreatureHighlighter highlighter)
    {
        _entry = entry;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;

        var indent = new Label { Text = "    " };
        AddChild(indent);

        var labels = new List<Label>();

        if (!string.IsNullOrEmpty(_entry.SourceName))
        {
            labels.Add(AppendLabel($"{_entry.SourceName} →", SourceColor));
        }

        var victimSuffix = string.IsNullOrEmpty(_entry.OwnerName) || _entry.OwnerName == _entry.VictimName
            ? _entry.VictimName
            : $"{_entry.VictimName} [{_entry.OwnerName}]";
        labels.Add(AppendLabel($" {victimSuffix}:", NeutralColor));

        if (_entry.HpLost > 0)
        {
            labels.Add(AppendLabel($" -{_entry.HpLost} HP", HpLostColor));
        }

        if (_entry.BlockedDamage > 0)
        {
            var prefix = _entry.HpLost > 0 ? " (" : " ";
            var suffix = _entry.HpLost > 0 ? " blocked)" : " blocked";
            labels.Add(AppendLabel($"{prefix}{_entry.BlockedDamage}{suffix}", BlockedColor));
        }

        if (_entry.WasKilled)
        {
            labels.Add(AppendLabel(" 💀", HpLostColor));
        }

        var sourceCombatId = _entry.SourceCombatId;
        var victimCombatId = _entry.VictimCombatId;
        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", HoverColor);
            _highlighter.Highlight(sourceCombatId);
            _highlighter.Highlight(victimCombatId);
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

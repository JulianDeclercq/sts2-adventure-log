using CombatLog.CombatLogCode.Events;
using Godot;

namespace CombatLog.CombatLogCode.UI.Rows;

public partial class DamageEntryRow : HBoxContainer
{
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
            labels.Add(AppendLabel($"{_entry.SourceName} →", DamageColors.Source));

        var victimSuffix = string.IsNullOrEmpty(_entry.OwnerName) || _entry.OwnerName == _entry.VictimName
            ? _entry.VictimName
            : $"{_entry.VictimName} [{_entry.OwnerName}]";
        labels.Add(AppendLabel($" {victimSuffix}:", DamageColors.Neutral));

        labels.AddRange(DamageColors.AppendDamageLabels(this, _entry.HpLost, _entry.BlockedDamage, _entry.WasKilled));

        var sourceCombatId = _entry.SourceCombatId;
        var victimCombatId = _entry.VictimCombatId;
        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", DamageColors.Hover);
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

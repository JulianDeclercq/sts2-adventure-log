using AdventureLog.AdventureLogCode.Events;
using Godot;

namespace AdventureLog.AdventureLogCode.UI.Rows;

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
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

        var indent = new Label { Text = "    " };
        AddChild(indent);

        List<Label> labels = [];

        if (!string.IsNullOrEmpty(_entry.SourceName))
            labels.Add(AppendLabel($"{NameTruncator.Short(_entry.SourceName)} →", DamageColors.Source));

        var shortVictim = NameTruncator.Short(_entry.VictimName);
        var victimSuffix = string.IsNullOrEmpty(_entry.OwnerName) || _entry.OwnerName == _entry.VictimName
            ? shortVictim
            : $"{shortVictim} [{_entry.OwnerName}]";
        labels.Add(AppendLabel($" {victimSuffix}:", DamageColors.Neutral));

        labels.AddRange(DamageColors.AppendDamageLabels(this, _entry.HpLost, _entry.BlockedDamage, _entry.WasKilled));

        var modifierLabel = DamageColors.AppendModifiersLabel(this, _entry.Modifiers);
        if (modifierLabel is not null) labels.Add(modifierLabel);

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

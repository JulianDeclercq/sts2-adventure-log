using Godot;

namespace AdventureLog.AdventureLogCode.UI.Rows;

public partial class DamageSubRow : HBoxContainer
{
    private static readonly Color VictimColor = new(0.7f, 0.6f, 0.5f);

    private readonly string _victimName;
    private readonly uint? _victimCombatId;
    private readonly uint? _sourceCombatId;
    private readonly int _hpLost;
    private readonly int _blocked;
    private readonly bool _killed;
    private readonly IReadOnlyList<string> _modifiers;
    private readonly CreatureHighlighter _highlighter;

    public DamageSubRow(
        string victimName, uint? victimCombatId, uint? sourceCombatId,
        int hpLost, int blocked, bool killed,
        IReadOnlyList<string> modifiers,
        CreatureHighlighter highlighter)
    {
        _victimName = victimName;
        _victimCombatId = victimCombatId;
        _sourceCombatId = sourceCombatId;
        _hpLost = hpLost;
        _blocked = blocked;
        _killed = killed;
        _modifiers = modifiers;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

        AddChild(new Label { Text = "    " });

        List<Label> labels = [];

        var victimLabel = new Label();
        victimLabel.Text = $"→ {NameTruncator.Short(_victimName)}:";
        victimLabel.AddThemeColorOverride("font_color", VictimColor);
        AddChild(victimLabel);
        labels.Add(victimLabel);

        labels.AddRange(DamageColors.AppendDamageLabels(this, _hpLost, _blocked, _killed));

        var modifierLabel = DamageColors.AppendModifiersLabel(this, _modifiers);
        if (modifierLabel is not null) labels.Add(modifierLabel);

        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", DamageColors.Hover);
            _highlighter.Highlight(_sourceCombatId);
            _highlighter.Highlight(_victimCombatId);
        };

        MouseExited += () =>
        {
            for (int i = 0; i < labels.Count; i++)
                labels[i].AddThemeColorOverride("font_color", originalColors[i]);
            _highlighter.Clear();
        };
    }
}

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
    private readonly CreatureHighlighter _highlighter;

    public DamageSubRow(
        string victimName, uint? victimCombatId, uint? sourceCombatId,
        int hpLost, int blocked, bool killed,
        CreatureHighlighter highlighter)
    {
        _victimName = victimName;
        _victimCombatId = victimCombatId;
        _sourceCombatId = sourceCombatId;
        _hpLost = hpLost;
        _blocked = blocked;
        _killed = killed;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

        AddChild(new Label { Text = "    " });

        var labels = new List<Label>();

        var victimLabel = new Label();
        victimLabel.Text = $"→ {NameTruncator.Short(_victimName)}:";
        victimLabel.AddThemeColorOverride("font_color", VictimColor);
        AddChild(victimLabel);
        labels.Add(victimLabel);

        labels.AddRange(DamageColors.AppendDamageLabels(this, _hpLost, _blocked, _killed));

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

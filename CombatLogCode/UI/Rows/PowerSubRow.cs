using CombatLog.CombatLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CombatLog.CombatLogCode.UI.Rows;

public partial class PowerSubRow : HBoxContainer
{
    private static readonly Color BuffColor = new(0.4f, 0.9f, 0.4f);
    private static readonly Color DebuffColor = new(0.85f, 0.35f, 0.85f);
    private static readonly Color TargetColor = new(0.7f, 0.6f, 0.5f);
    private static readonly Color HoverColor = new(1.0f, 1.0f, 0.6f);
    private const float IconSize = 16;

    private readonly PowerReceivedEvent _entry;
    private readonly CreatureHighlighter _highlighter;

    public PowerSubRow(PowerReceivedEvent entry, CreatureHighlighter highlighter)
    {
        _entry = entry;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;

        AddChild(new Label { Text = "    " });

        var labels = new List<Label>();
        var powerColor = _entry.Type == PowerType.Buff ? BuffColor : DebuffColor;

        // Drop "→ target:" prefix when self-applied (applier == owner): redundant,
        // mirrors relic sub-row behavior for self-targets.
        var isSelfTarget = _entry.ApplierCombatId.HasValue
                           && _entry.ApplierCombatId == _entry.OwnerCreatureCombatId;

        if (!isSelfTarget && !string.IsNullOrEmpty(_entry.OwnerCreatureName))
        {
            var targetLabel = new Label();
            targetLabel.Text = $"\u2192 {_entry.OwnerCreatureName}:";
            targetLabel.AddThemeColorOverride("font_color", TargetColor);
            AddChild(targetLabel);
            labels.Add(targetLabel);
        }

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

        var effectText = _entry.StackType == PowerStackType.Counter
            ? $"+{_entry.Delta} {_entry.PowerTitle}"
            : $"{_entry.PowerTitle}";
        if (_entry.NewTotal != _entry.Delta)
            effectText += $" (={_entry.NewTotal})";

        var effectLabel = new Label();
        effectLabel.Text = effectText;
        effectLabel.AddThemeColorOverride("font_color", powerColor);
        AddChild(effectLabel);
        labels.Add(effectLabel);

        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", HoverColor);
            _highlighter.Highlight(_entry.OwnerCreatureCombatId);
        };

        MouseExited += () =>
        {
            for (int i = 0; i < labels.Count; i++)
                labels[i].AddThemeColorOverride("font_color", originalColors[i]);
            _highlighter.Clear();
        };
    }
}

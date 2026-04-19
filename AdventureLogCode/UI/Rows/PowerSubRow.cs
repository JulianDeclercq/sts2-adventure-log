using AdventureLog.AdventureLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace AdventureLog.AdventureLogCode.UI.Rows;

public partial class PowerSubRow : HBoxContainer
{
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
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

        AddChild(new Label { Text = "    " });

        var labels = new List<Label>();
        var isNegative = _entry.StackType == PowerStackType.Counter && _entry.Delta < 0;
        var powerColor = isNegative
            ? PowerColors.Negative
            : (_entry.Type == PowerType.Buff ? PowerColors.Buff : PowerColors.Debuff);

        // Drop "→ target:" prefix when self-applied (applier == owner): redundant,
        // mirrors relic sub-row behavior for self-targets.
        var isSelfTarget = _entry.ApplierCombatId.HasValue
                           && _entry.ApplierCombatId == _entry.OwnerCreatureCombatId;

        if (!isSelfTarget && !string.IsNullOrEmpty(_entry.OwnerCreatureName))
        {
            var targetLabel = new Label();
            targetLabel.Text = $"\u2192 {NameTruncator.Short(_entry.OwnerCreatureName)}:";
            targetLabel.AddThemeColorOverride("font_color", PowerColors.Target);
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

        var shortTitle = NameTruncator.Short(_entry.PowerTitle);
        var effectText = _entry.StackType == PowerStackType.Counter
            ? $"{(_entry.Delta >= 0 ? "+" : "")}{_entry.Delta} {shortTitle}"
            : shortTitle;
        if (_entry.NewTotal != _entry.Delta)
            effectText += $" ({_entry.NewTotal})";

        var effectLabel = new Label();
        effectLabel.Text = effectText;
        effectLabel.AddThemeColorOverride("font_color", powerColor);
        AddChild(effectLabel);
        labels.Add(effectLabel);

        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", PowerColors.Hover);
            _highlighter.Highlight(_entry.OwnerCreatureCombatId);
            if (_entry.Power is not null)
                try
                {
                    var tips = _entry.Power.HoverTips.ToList();
                    // PowerModel.HoverTips returns empty when IsVisible=false (e.g. Vulnerable mid-re-apply,
                    // ally pets). Fall back to DumbHoverTip so the row always has a description.
                    if (tips.Count == 0) tips.Add(_entry.Power.DumbHoverTip);
                    var tip = NHoverTipSet.CreateAndShow(this, tips);
                    if (tip is not null) HoverTipHelper.PositionLeftOfCursor(this, tip);
                }
                catch { }
        };

        MouseExited += () =>
        {
            for (int i = 0; i < labels.Count; i++)
                labels[i].AddThemeColorOverride("font_color", originalColors[i]);
            _highlighter.Clear();
            try { NHoverTipSet.Remove(this); } catch { }
        };

        TreeExiting += () =>
        {
            try { NHoverTipSet.Remove(this); } catch { }
        };
    }
}

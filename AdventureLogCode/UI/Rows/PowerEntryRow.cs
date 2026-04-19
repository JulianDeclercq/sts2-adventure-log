using AdventureLog.AdventureLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace AdventureLog.AdventureLogCode.UI.Rows;

public partial class PowerEntryRow : HBoxContainer
{
    private const float IconSize = 20;

    private readonly PowerReceivedEvent _entry;
    private readonly CreatureHighlighter _highlighter;

    public PowerEntryRow(PowerReceivedEvent entry, CreatureHighlighter highlighter)
    {
        _entry = entry;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

        var labels = new List<Label>();
        var isNegative = _entry.StackType == PowerStackType.Counter && _entry.Delta < 0;
        var powerColor = isNegative
            ? PowerColors.Negative
            : (_entry.Type == PowerType.Buff ? PowerColors.Buff : PowerColors.Debuff);

        if (!string.IsNullOrEmpty(_entry.OwnerCreatureName))
        {
            labels.Add(AppendLabel($"{NameTruncator.Short(_entry.OwnerCreatureName)}:", PowerColors.Target));
        }

        if (_entry.Icon is not null)
        {
            var rect = new TextureRect
            {
                Texture = _entry.Icon,
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            AddChild(rect);
        }

        var shortTitle = NameTruncator.Short(_entry.PowerTitle);
        var nameText = _entry.StackType == PowerStackType.Counter
            ? $"{(_entry.Delta >= 0 ? "+" : "")}{_entry.Delta} {shortTitle}"
            : shortTitle;

        if (_entry.NewTotal != _entry.Delta)
            nameText += $" ({_entry.NewTotal})";

        if (!string.IsNullOrEmpty(_entry.OwnerName))
            nameText += $" [{_entry.OwnerName}]";

        labels.Add(AppendLabel(nameText, powerColor));

        if (_entry.ApplierCombatId.HasValue && _entry.ApplierCombatId != _entry.OwnerCreatureCombatId)
        {
            labels.Add(AppendLabel($"\u2190 {NameTruncator.Short(_entry.ApplierName)}", PowerColors.Target));
        }

        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", PowerColors.Hover);
            _highlighter.Highlight(_entry.OwnerCreatureCombatId);
            if (_entry.ApplierCombatId.HasValue && _entry.ApplierCombatId != _entry.OwnerCreatureCombatId)
                _highlighter.Highlight(_entry.ApplierCombatId);
            if (_entry.Power is not null)
                try
                {
                    var tips = _entry.Power.HoverTips.ToList();
                    // Some powers (ally pets etc.) return empty HoverTips via visibility check — fall back to the raw description.
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

    private Label AppendLabel(string text, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color);
        AddChild(label);
        return label;
    }
}

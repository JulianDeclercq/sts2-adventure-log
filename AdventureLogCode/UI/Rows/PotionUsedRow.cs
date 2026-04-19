using AdventureLog.AdventureLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace AdventureLog.AdventureLogCode.UI.Rows;

public partial class PotionUsedRow : HBoxContainer
{
    private static readonly Color PotionColor = new(0.7f, 1.0f, 0.7f);
    private static readonly Color TargetColor = new(0.7f, 0.6f, 0.5f);
    private static readonly Color HoverColor = new(1.0f, 0.95f, 0.5f);
    private const float IconSize = 20;

    private readonly PotionUsedEvent _entry;
    private readonly CreatureHighlighter _highlighter;

    public PotionUsedRow(PotionUsedEvent entry, CreatureHighlighter highlighter)
    {
        _entry = entry;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

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

        List<Label> labels = [];

        var shortName = NameTruncator.Short(_entry.PotionTitle);
        var nameText = string.IsNullOrEmpty(_entry.OwnerName)
            ? shortName
            : $"{shortName} [{_entry.OwnerName}]";
        labels.Add(AppendLabel(nameText, PotionColor));

        if (!string.IsNullOrEmpty(_entry.TargetName))
            labels.Add(AppendLabel($"\u2192 {NameTruncator.Short(_entry.TargetName!)}", TargetColor));

        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();
        var targetId = _entry.TargetCombatId;

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", HoverColor);
            if (targetId.HasValue) _highlighter.Highlight(targetId);
            if (_entry.Potion is not null)
                try
                {
                    var tip = NHoverTipSet.CreateAndShow(this, _entry.Potion.HoverTips);
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

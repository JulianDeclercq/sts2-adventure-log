using AdventureLog.AdventureLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace AdventureLog.AdventureLogCode.UI.Rows;

public partial class RelicEntryRow : HBoxContainer
{
    private static readonly Color RelicColor = new(1.0f, 0.85f, 0.4f);
    private static readonly Color TargetColor = new(0.7f, 0.6f, 0.5f);
    private static readonly Color HoverColor = new(1.0f, 0.95f, 0.5f);
    private const float IconSize = 20;

    private readonly RelicProcEvent _entry;
    private readonly CreatureHighlighter _highlighter;
    private NRelicInventoryHolder? _hoveredHolder;

    public RelicEntryRow(RelicProcEvent entry, CreatureHighlighter highlighter)
    {
        _entry = entry;
        _highlighter = highlighter;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

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

        var shortName = NameTruncator.Short(_entry.RelicName);
        var nameText = string.IsNullOrEmpty(_entry.OwnerName)
            ? shortName
            : $"{shortName} [{_entry.OwnerName}]";
        labels.Add(AppendLabel(nameText, RelicColor));

        if (_entry.TargetNames.Count > 0)
        {
            var targets = string.Join(", ", _entry.TargetNames.Select(n => NameTruncator.Short(n)));
            labels.Add(AppendLabel($"\u2192 {targets}", TargetColor));
        }

        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();
        var targetIds = _entry.TargetCombatIds;

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", HoverColor);
            foreach (var id in targetIds) _highlighter.Highlight(id);
            if (_entry.IsLocal)
            {
                _hoveredHolder = FindRelicHolder(_entry.RelicId);
                _hoveredHolder?.OnFocus();
            }
            else if (_entry.Relic is not null)
            {
                // Teammate relics aren't in the local RelicBar, so RelicBar.OnFocus can't
                // show their tooltip. Build one ourselves from the RelicModel.
                try
                {
                    var tip = NHoverTipSet.CreateAndShow(this, _entry.Relic.HoverTips);
                    if (tip is not null) HoverTipHelper.PositionLeftOfCursor(this, tip);
                }
                catch { }
            }
        };

        MouseExited += () =>
        {
            for (int i = 0; i < labels.Count; i++)
                labels[i].AddThemeColorOverride("font_color", originalColors[i]);
            _highlighter.Clear();
            if (_hoveredHolder is not null && GodotObject.IsInstanceValid(_hoveredHolder))
                _hoveredHolder.OnUnfocus();
            _hoveredHolder = null;
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

    private NRelicInventoryHolder? FindRelicHolder(string relicId)
    {
        var root = GetTree()?.Root;
        if (root is null) return null;

        foreach (var node in root.FindChildren("*", recursive: true, owned: false))
        {
            if (node is not NRelicInventory inv) continue;
            foreach (var holder in inv.RelicNodes)
            {
                if (holder._relic?.Model?.Id.Entry == relicId)
                    return holder;
            }
        }
        return null;
    }
}

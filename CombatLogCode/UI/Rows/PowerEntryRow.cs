using CombatLog.CombatLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CombatLog.CombatLogCode.UI.Rows;

public partial class PowerEntryRow : HBoxContainer
{
    private static readonly Color BuffColor = new(0.4f, 0.9f, 0.4f);
    private static readonly Color DebuffColor = new(0.85f, 0.35f, 0.85f);
    private static readonly Color NegativeColor = new(0.9f, 0.4f, 0.4f);
    private static readonly Color TargetColor = new(0.7f, 0.6f, 0.5f);
    private static readonly Color HoverColor = new(1.0f, 1.0f, 0.6f);
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

        var labels = new List<Label>();
        var isNegative = _entry.StackType == PowerStackType.Counter && _entry.Delta < 0;
        var powerColor = isNegative
            ? NegativeColor
            : (_entry.Type == PowerType.Buff ? BuffColor : DebuffColor);

        if (!string.IsNullOrEmpty(_entry.OwnerCreatureName))
        {
            labels.Add(AppendLabel($"\u2192 {_entry.OwnerCreatureName}:", TargetColor));
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

        var nameText = _entry.StackType == PowerStackType.Counter
            ? $"{(_entry.Delta >= 0 ? "+" : "")}{_entry.Delta} {_entry.PowerTitle}"
            : $"{_entry.PowerTitle}";

        if (_entry.NewTotal != _entry.Delta)
            nameText += $" (={_entry.NewTotal})";

        if (!string.IsNullOrEmpty(_entry.OwnerName))
            nameText += $" [{_entry.OwnerName}]";

        labels.Add(AppendLabel(nameText, powerColor));

        if (_entry.ApplierCombatId.HasValue && _entry.ApplierCombatId != _entry.OwnerCreatureCombatId)
        {
            labels.Add(AppendLabel($"\u2190 {_entry.ApplierName}", TargetColor));
        }

        var originalColors = labels.Select(l => l.GetThemeColor("font_color")).ToList();

        MouseEntered += () =>
        {
            foreach (var l in labels) l.AddThemeColorOverride("font_color", HoverColor);
            _highlighter.Highlight(_entry.OwnerCreatureCombatId);
            if (_entry.ApplierCombatId.HasValue && _entry.ApplierCombatId != _entry.OwnerCreatureCombatId)
                _highlighter.Highlight(_entry.ApplierCombatId);
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

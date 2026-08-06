using Godot;

namespace AdventureLog.AdventureLogCode.UI.Rows;

internal static class DamageColors
{
    public static readonly Color HpLost = new(0.9f, 0.3f, 0.3f);
    public static readonly Color Blocked = new(0.6f, 0.7f, 0.9f);
    public static readonly Color Neutral = new(0.8f, 0.8f, 0.8f);
    public static readonly Color Source = new(0.75f, 0.65f, 0.55f);
    public static readonly Color Hover = new(1.0f, 0.95f, 0.5f);
    public static readonly Color Modifier = new(0.55f, 0.75f, 0.95f);

    public static List<Label> AppendDamageLabels(Container target, int hpLost, int blocked, bool killed)
    {
        List<Label> labels = [];

        if (hpLost > 0)
            labels.Add(AddLabel(target, $" -{hpLost} HP", HpLost));

        if (blocked > 0)
        {
            var prefix = hpLost > 0 ? " (" : " ";
            var suffix = hpLost > 0 ? " blocked)" : " blocked";
            labels.Add(AddLabel(target, $"{prefix}{blocked}{suffix}", Blocked));
        }

        if (killed)
            labels.Add(AddLabel(target, " 💀", HpLost));

        if (labels.Count == 0)
            labels.Add(AddLabel(target, " no damage", Neutral));

        return labels;
    }

    public static Label? AppendModifiersLabel(Container target, IReadOnlyList<string>? modifiers)
    {
        if (modifiers is null || modifiers.Count == 0) return null;
        return AddLabel(target, $" [{string.Join(", ", modifiers)}]", Modifier);
    }

    private static Label AddLabel(Container target, string text, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color);
        target.AddChild(label);
        return label;
    }
}

using AdventureLog.AdventureLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace AdventureLog.AdventureLogCode.UI.Rows;

public partial class CardDrawRow : HBoxContainer
{
    private static readonly Color LabelColor = new(0.75f, 0.85f, 1.0f);
    private static readonly Color HoverColor = new(1.0f, 0.95f, 0.5f);
    private static readonly Color NoCardColor = new(0.6f, 0.6f, 0.6f);

    private const float CardIconSize = 20;

    private readonly CardDrawEvent _entry;
    private readonly Action<CardModel> _openInspect;

    public CardDrawRow(CardDrawEvent entry, Action<CardModel> openInspect)
    {
        _entry = entry;
        _openInspect = openInspect;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

        AddChild(new Label { Text = "    " });

        var arrow = new Label { Text = "\u21E3" };
        arrow.AddThemeColorOverride("font_color", LabelColor);
        AddChild(arrow);

        if (_entry.DrawnCard is not null)
        {
            var tinyCard = TinyCardFactory.Build(_entry.DrawnCard, CardIconSize);
            if (tinyCard is not null) AddChild(tinyCard);
        }

        var nameLabel = new Label();
        var shortCardName = NameTruncator.Short(_entry.DrawnCardName);
        var nameText = string.IsNullOrEmpty(_entry.OwnerName)
            ? $"{shortCardName} drawn"
            : $"{shortCardName} drawn [{_entry.OwnerName}]";
        nameLabel.Text = nameText;
        var baseColor = _entry.DrawnCard is null ? NoCardColor : LabelColor;
        nameLabel.AddThemeColorOverride("font_color", baseColor);
        AddChild(nameLabel);

        var card = _entry.DrawnCard;

        MouseEntered += () =>
        {
            nameLabel.AddThemeColorOverride("font_color", HoverColor);
            if (card is not null)
                try
                {
                    var hoverTip = new CardHoverTip(card);
                    NHoverTipSet.CreateAndShow(this, hoverTip, HoverTipAlignment.Left);
                }
                catch { }
        };

        MouseExited += () =>
        {
            nameLabel.AddThemeColorOverride("font_color", baseColor);
            try { NHoverTipSet.Remove(this); } catch { }
        };

        GuiInput += (@event) =>
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && card is not null)
            {
                try { NHoverTipSet.Remove(this); } catch { }
                _openInspect(card);
            }
        };

        TreeExiting += () =>
        {
            try { NHoverTipSet.Remove(this); } catch { }
        };
    }
}

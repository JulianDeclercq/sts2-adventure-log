using CombatLog.CombatLogCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace CombatLog.CombatLogCode.UI.Rows;

public partial class CardEntryRow : HBoxContainer
{
    private static readonly Color CardLinkColor = new(0.6f, 0.85f, 1.0f);
    private static readonly Color CardLinkHoverColor = new(1.0f, 0.95f, 0.5f);
    private static readonly Color NoCardColor = new(0.6f, 0.6f, 0.6f);

    private const string TinyCardScenePath = "res://scenes/cards/tiny_card.tscn";
    private const float CardIconSize = 24;
    private static PackedScene? _tinyCardScene;

    private readonly CardPlayEvent _entry;
    private readonly IReadOnlyList<DamageReceivedEvent> _damages;
    private readonly CreatureHighlighter _highlighter;
    private readonly Action<CardModel> _openInspect;

    public CardEntryRow(
        CardPlayEvent entry,
        IReadOnlyList<DamageReceivedEvent>? damages,
        CreatureHighlighter highlighter,
        Action<CardModel> openInspect)
    {
        _entry = entry;
        _damages = damages ?? Array.Empty<DamageReceivedEvent>();
        _highlighter = highlighter;
        _openInspect = openInspect;
    }

    public override void _Ready()
    {
        var displayText = string.IsNullOrEmpty(_entry.OwnerName)
            ? _entry.CardName
            : $"{_entry.CardName} [{_entry.OwnerName}]";

        if (_entry.Card is null)
        {
            var fallback = new Label();
            fallback.Text = $"    {displayText}";
            fallback.AddThemeColorOverride("font_color", NoCardColor);
            AddChild(fallback);
            return;
        }

        var card = _entry.Card;
        var rarityColor = GetRarityColor(card.Rarity);

        AddThemeConstantOverride("separation", 4);
        MouseFilter = MouseFilterEnum.Stop;

        _tinyCardScene ??= GD.Load<PackedScene>(TinyCardScenePath);
        if (_tinyCardScene is not null)
        {
            var tinyCard = _tinyCardScene.Instantiate<NTinyCard>();
            tinyCard.CustomMinimumSize = new Vector2(CardIconSize, CardIconSize);
            tinyCard.Scale = new Vector2(0.4f, 0.4f);
            AddChild(tinyCard);
            var cardRef = card;
            tinyCard.Ready += () => tinyCard.SetCard(cardRef);
        }

        var nameLabel = new Label();
        nameLabel.Text = displayText;
        nameLabel.AddThemeColorOverride("font_color", rarityColor);
        AddChild(nameLabel);

        var playerCombatId = _entry.PlayerCombatId;
        var fallbackTargetId = _entry.TargetCombatId;
        var victimIds = _damages
            .Where(d => d.VictimCombatId.HasValue)
            .Select(d => d.VictimCombatId)
            .Distinct()
            .ToList();

        MouseEntered += () =>
        {
            nameLabel.AddThemeColorOverride("font_color", CardLinkHoverColor);
            var hoverTip = new CardHoverTip(card);
            NHoverTipSet.CreateAndShow(this, hoverTip, HoverTipAlignment.Left);
            _highlighter.Highlight(playerCombatId);
            if (victimIds.Count > 0)
                foreach (var vid in victimIds) _highlighter.Highlight(vid);
            else
                _highlighter.Highlight(fallbackTargetId);
        };

        MouseExited += () =>
        {
            nameLabel.AddThemeColorOverride("font_color", rarityColor);
            NHoverTipSet.Remove(this);
            _highlighter.Clear();
        };

        GuiInput += (@event) =>
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                NHoverTipSet.Remove(this);
                _openInspect(card);
            }
        };
    }

    private static Color GetRarityColor(CardRarity rarity) => rarity switch
    {
        CardRarity.Basic => new Color(0.7f, 0.7f, 0.7f),
        CardRarity.Common => new Color(1f, 1f, 1f),
        CardRarity.Uncommon => new Color(0.5f, 0.9f, 0.3f),
        CardRarity.Rare => new Color(1f, 0.85f, 0.2f),
        _ => CardLinkColor
    };
}

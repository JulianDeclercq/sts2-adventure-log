using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace CombatLog.CombatLogCode.UI;

/// <summary>
/// A toggleable panel (press H) showing all cards played during the run.
/// Injected into the scene tree via a Harmony patch on a suitable game node.
/// </summary>
public partial class CombatLogPanel : PanelContainer
{
    private VBoxContainer _list = null!;
    private ScrollContainer _scroll = null!;
    private Label _header = null!;
    private bool _isShown;
    private int _lastKnownCount;

    private static readonly Color CardLinkColor = new(0.6f, 0.85f, 1.0f);
    private static readonly Color CardLinkHoverColor = new(1.0f, 0.95f, 0.5f);
    private static readonly Color TargetNameColor = new(0.7f, 0.6f, 0.5f);

    private NCreature? _highlightedCreature;

    private static CombatLogPanel? _instance;
    public static CombatLogPanel? Instance => _instance;

    public override void _Ready()
    {
        _instance = this;

        // Panel styling
        CustomMinimumSize = new Vector2(300, 0);
        AnchorLeft = 1.0f;
        AnchorRight = 1.0f;
        AnchorTop = 0.07f;
        AnchorBottom = 0.78f;
        OffsetLeft = -310;
        OffsetRight = -10;
        OffsetTop = 0;
        OffsetBottom = 0;
        GrowHorizontal = GrowDirection.Begin;

        // Semi-transparent dark background
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        styleBox.BorderColor = new Color(0.4f, 0.4f, 0.6f, 0.8f);
        styleBox.SetBorderWidthAll(2);
        styleBox.SetCornerRadiusAll(8);
        styleBox.SetContentMarginAll(10);
        AddThemeStyleboxOverride("panel", styleBox);

        var vbox = new VBoxContainer();
        AddChild(vbox);

        // Header
        _header = new Label();
        _header.Text = "Combat Log (F to toggle)";
        _header.HorizontalAlignment = HorizontalAlignment.Center;
        _header.AddThemeColorOverride("font_color", new Color(0.9f, 0.8f, 0.3f));
        vbox.AddChild(_header);

        // Separator
        vbox.AddChild(new HSeparator());

        // Scrollable list
        _scroll = new ScrollContainer();
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(_scroll);

        _list = new VBoxContainer();
        _list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.AddChild(_list);

        Visible = false;
        _isShown = false;

        // Listen for changes
        CombatLogTracker.OnHistoryChanged += OnHistoryChanged;
    }

    public override void _ExitTree()
    {
        CombatLogTracker.OnHistoryChanged -= OnHistoryChanged;
        if (_instance == this) _instance = null;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.F)
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Toggle()
    {
        _isShown = !_isShown;
        Visible = _isShown;
        if (_isShown) RefreshList();
    }

    private void OnHistoryChanged()
    {
        if (_isShown) RefreshList();
    }

    private void RefreshList()
    {
        var history = CombatLogTracker.History;

        // Only rebuild if count changed
        if (history.Count == _lastKnownCount) return;

        // Clear existing
        foreach (var child in _list.GetChildren())
            child.QueueFree();

        int lastCombat = -1;
        int lastTurn = -1;

        // Iterate in reverse so the most recent combat/turn appears at the top
        for (int i = history.Count - 1; i >= 0; i--)
        {
            var entry = history[i];

            // Combat header
            if (entry.CombatNumber != lastCombat)
            {
                lastCombat = entry.CombatNumber;
                lastTurn = -1;
                var combatLabel = new Label();
                combatLabel.Text = $"--- Combat {entry.CombatNumber} ---";
                combatLabel.HorizontalAlignment = HorizontalAlignment.Center;
                combatLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1.0f));
                _list.AddChild(combatLabel);
            }

            // Turn header
            if (entry.TurnNumber != lastTurn)
            {
                lastTurn = entry.TurnNumber;
                var turnLabel = new Label();
                turnLabel.Text = $"  Turn {entry.TurnNumber}:";
                turnLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.5f));
                _list.AddChild(turnLabel);
            }

            // Card entry (interactive)
            var cardControl = CreateCardEntry(entry);
            _list.AddChild(cardControl);
        }

        _lastKnownCount = history.Count;

        // Scroll to top to show most recent
        CallDeferred(nameof(ScrollToTop));
    }

    private void ScrollToTop()
    {
        _scroll.ScrollVertical = 0;
    }

    private static Color GetRarityColor(CardRarity rarity) => rarity switch
    {
        CardRarity.Basic => new Color(0.7f, 0.7f, 0.7f),
        CardRarity.Common => new Color(1f, 1f, 1f),
        CardRarity.Uncommon => new Color(0.5f, 0.9f, 0.3f),
        CardRarity.Rare => new Color(1f, 0.85f, 0.2f),
        _ => CardLinkColor
    };

    private const string TinyCardScenePath = "res://scenes/cards/tiny_card.tscn";
    private static PackedScene? _tinyCardScene;
    private const float CardIconSize = 24;

    private Control CreateCardEntry(CombatLogTracker.CardPlayEntry entry)
    {
        var displayText = string.IsNullOrEmpty(entry.PlayerName)
            ? entry.CardName
            : $"{entry.CardName} [{entry.PlayerName}]";

        if (entry.Card is not null)
        {
            var card = entry.Card;
            var rarityColor = GetRarityColor(card.Rarity);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 4);
            hbox.MouseFilter = Control.MouseFilterEnum.Stop;

            // Card icon: use the game's NTinyCard scene
            _tinyCardScene ??= GD.Load<PackedScene>(TinyCardScenePath);
            if (_tinyCardScene is not null)
            {
                var tinyCard = _tinyCardScene.Instantiate<NTinyCard>();
                tinyCard.CustomMinimumSize = new Vector2(CardIconSize, CardIconSize);
                tinyCard.Scale = new Vector2(0.4f, 0.4f);
                hbox.AddChild(tinyCard);
                // Defer SetCard until after _Ready() (Ready fires after _Ready completes)
                var cardRef = card;
                tinyCard.Ready += () => tinyCard.SetCard(cardRef);
            }

            // Card name label
            var label = new Label();
            label.Text = displayText;
            label.AddThemeColorOverride("font_color", rarityColor);
            hbox.AddChild(label);

            // Target name (only for single-target cards)
            if (!string.IsNullOrEmpty(entry.TargetName))
            {
                var targetLabel = new Label();
                targetLabel.Text = $"→ {entry.TargetName}";
                targetLabel.AddThemeColorOverride("font_color", TargetNameColor);
                hbox.AddChild(targetLabel);
            }

            // Hover: highlight + show native game tooltip + highlight target creature
            var targetCombatId = entry.TargetCombatId;
            hbox.MouseEntered += () =>
            {
                label.AddThemeColorOverride("font_color", CardLinkHoverColor);
                var hoverTip = new CardHoverTip(card);
                NHoverTipSet.CreateAndShow(hbox, hoverTip, HoverTipAlignment.Left);
                HighlightCreature(targetCombatId);
            };

            hbox.MouseExited += () =>
            {
                label.AddThemeColorOverride("font_color", rarityColor);
                NHoverTipSet.Remove(hbox);
                ClearCreatureHighlight();
            };

            // Click: open the game's full inspect-card screen
            hbox.GuiInput += (@event) =>
            {
                if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                {
                    NHoverTipSet.Remove(hbox);
                    OpenInspectScreen(card);
                }
            };

            return hbox;
        }
        else
        {
            // Non-interactive entry (no card reference)
            var label = new Label();
            label.Text = $"    {displayText}";
            label.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            return label;
        }
    }

    private void HighlightCreature(uint? targetCombatId)
    {
        if (targetCombatId is null) return;

        var combatRoom = FindCombatRoom();
        if (combatRoom is null) return;

        foreach (var creatureNode in combatRoom.CreatureNodes)
        {
            if (creatureNode.Entity?.CombatId == targetCombatId)
            {
                _highlightedCreature = creatureNode;
                creatureNode.ShowSingleSelectReticle();
                return;
            }
        }
    }

    private void ClearCreatureHighlight()
    {
        if (_highlightedCreature is not null && GodotObject.IsInstanceValid(_highlightedCreature))
        {
            _highlightedCreature.HideSingleSelectReticle();
        }
        _highlightedCreature = null;
    }

    private NCombatRoom? FindCombatRoom()
    {
        var root = GetTree()?.Root;
        if (root is null) return null;

        foreach (var node in root.FindChildren("*", recursive: true, owned: false))
        {
            if (node is NCombatRoom room) return room;
        }
        return null;
    }

    private void OpenInspectScreen(CardModel card)
    {
        var inspectScreen = FindInspectCardScreen();
        if (inspectScreen is null)
        {
            inspectScreen = NInspectCardScreen.Create();
            if (inspectScreen is null)
            {
                GD.PrintErr("[CombatLog] Failed to create NInspectCardScreen.");
                return;
            }
            GetTree().Root.AddChild(inspectScreen);
        }

        try
        {
            inspectScreen.Open(new System.Collections.Generic.List<CardModel> { card }, 0);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CombatLog] Failed to open inspect screen: {e.Message}");
        }
    }

    private NInspectCardScreen? FindInspectCardScreen()
    {
        var root = GetTree()?.Root;
        if (root is null) return null;

        // Search the whole tree (owned: false because the screen isn't owned by our scene)
        foreach (var node in root.FindChildren("*", nameof(NInspectCardScreen), recursive: true, owned: false))
        {
            if (node is NInspectCardScreen screen) return screen;
        }
        return null;
    }
}

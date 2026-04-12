using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

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
    private Control? _cardPreview;

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
        _header.Text = "Combat Log (H to toggle)";
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
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.H)
        {
            _isShown = !_isShown;
            Visible = _isShown;
            if (_isShown) RefreshList();
            else DismissCardPreview();
            GetViewport().SetInputAsHandled();
        }
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

    private Control CreateCardEntry(CombatLogTracker.CardPlayEntry entry)
    {
        var label = new Label();
        label.Text = $"    {entry.CardName}";
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        label.MouseFilter = Control.MouseFilterEnum.Stop;

        if (entry.Card is not null)
        {
            var card = entry.Card;

            // Hover: show native game tooltip
            label.MouseEntered += () =>
            {
                label.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.4f));
                var hoverTip = new CardHoverTip(card);
                NHoverTipSet.CreateAndShow(label, hoverTip, HoverTipAlignment.None);
            };

            label.MouseExited += () =>
            {
                label.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
                NHoverTipSet.Remove(label);
            };

            // Click: show full card preview
            label.GuiInput += (@event) =>
            {
                if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                    ToggleCardPreview(card);
            };
        }

        return label;
    }

    private void ToggleCardPreview(CardModel card)
    {
        // If clicking same card or any card while preview is open, dismiss first
        DismissCardPreview();

        // Load game's card hover scene
        var container = PreloadManager.Cache
            .GetScene("res://scenes/ui/card_hover_tip.tscn")
            .Instantiate<Control>();

        var nCard = container.GetNode<NCard>("%Card");

        // Position to the left of the panel
        container.Position = new Vector2(-350, 100);

        _cardPreview = container;
        AddChild(container);

        container.TreeEntered += () =>
        {
            Callable.From(() =>
            {
                nCard.Model = card;
                nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            }).CallDeferred();
        };
    }

    private void DismissCardPreview()
    {
        if (_cardPreview is not null)
        {
            _cardPreview.QueueFree();
            _cardPreview = null;
        }
    }
}

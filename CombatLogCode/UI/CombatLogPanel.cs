using Godot;

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

    private static CombatLogPanel? _instance;
    public static CombatLogPanel? Instance => _instance;

    public override void _Ready()
    {
        _instance = this;

        // Panel styling
        CustomMinimumSize = new Vector2(300, 500);
        AnchorLeft = 1.0f;
        AnchorRight = 1.0f;
        AnchorTop = 0.0f;
        AnchorBottom = 1.0f;
        OffsetLeft = -310;
        OffsetRight = -10;
        OffsetTop = 10;
        OffsetBottom = -10;
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

        foreach (var entry in history)
        {
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

            // Card entry
            var cardLabel = new Label();
            cardLabel.Text = $"    {entry.CardName}";
            cardLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            _list.AddChild(cardLabel);
        }

        _lastKnownCount = history.Count;

        // Auto-scroll to bottom
        CallDeferred(nameof(ScrollToBottom));
    }

    private void ScrollToBottom()
    {
        _scroll.ScrollVertical = (int)_scroll.GetVScrollBar().MaxValue;
    }
}

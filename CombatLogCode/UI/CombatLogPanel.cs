using CombatLog.CombatLogCode.Events;
using CombatLog.CombatLogCode.UI.Rows;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace CombatLog.CombatLogCode.UI;

/// <summary>
/// Toggleable panel (F) showing events logged during the run.
/// Injected by UiInjectionPatch. Dispatches each LogEvent to its row type.
/// </summary>
public partial class CombatLogPanel : PanelContainer
{
    private VBoxContainer _list = null!;
    private ScrollContainer _scroll = null!;
    private Label _header = null!;
    private CreatureHighlighter _highlighter = null!;
    private bool _isShown;
    private int _lastKnownCount;

    private static CombatLogPanel? _instance;
    public static CombatLogPanel? Instance => _instance;

    public override void _Ready()
    {
        _instance = this;
        _highlighter = new CreatureHighlighter(this);

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

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        styleBox.BorderColor = new Color(0.4f, 0.4f, 0.6f, 0.8f);
        styleBox.SetBorderWidthAll(2);
        styleBox.SetCornerRadiusAll(8);
        styleBox.SetContentMarginAll(10);
        AddThemeStyleboxOverride("panel", styleBox);

        var vbox = new VBoxContainer();
        AddChild(vbox);

        _header = new Label();
        _header.Text = "Combat Log (F to toggle)";
        _header.HorizontalAlignment = HorizontalAlignment.Center;
        _header.AddThemeColorOverride("font_color", new Color(0.9f, 0.8f, 0.3f));
        vbox.AddChild(_header);

        vbox.AddChild(new HSeparator());

        _scroll = new ScrollContainer();
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(_scroll);

        _list = new VBoxContainer();
        _list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.AddChild(_list);

        Visible = false;
        _isShown = false;

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
        if (history.Count == _lastKnownCount) return;

        foreach (var child in _list.GetChildren())
            child.QueueFree();

        int lastCombat = -1;
        int lastTurn = -1;

        for (int i = history.Count - 1; i >= 0; i--)
        {
            var ev = history[i];

            if (ev.CombatNumber != lastCombat)
            {
                lastCombat = ev.CombatNumber;
                lastTurn = -1;
                _list.AddChild(BuildCombatHeader(ev.CombatNumber));
            }

            if (ev.TurnNumber != lastTurn)
            {
                lastTurn = ev.TurnNumber;
                _list.AddChild(BuildTurnHeader(ev.TurnNumber));
            }

            var row = BuildRow(ev);
            if (row is not null) _list.AddChild(row);
        }

        _lastKnownCount = history.Count;
        CallDeferred(nameof(ScrollToTop));
    }

    private void ScrollToTop() => _scroll.ScrollVertical = 0;

    private static Label BuildCombatHeader(int combatNumber)
    {
        var label = new Label();
        label.Text = $"--- Combat {combatNumber} ---";
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1.0f));
        return label;
    }

    private static Label BuildTurnHeader(int turnNumber)
    {
        var label = new Label();
        label.Text = $"  Turn {turnNumber}:";
        label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.5f));
        return label;
    }

    private Control? BuildRow(LogEvent ev) => ev switch
    {
        CardPlayEvent card => new CardEntryRow(card, _highlighter, OpenInspectScreen),
        DamageReceivedEvent dmg => new DamageEntryRow(dmg, _highlighter),
        _ => null
    };

    private void OpenInspectScreen(CardModel card)
    {
        var inspectScreen = FindInspectCardScreen() ?? NInspectCardScreen.Create();
        if (inspectScreen is null)
        {
            GD.PrintErr("[CombatLog] Failed to create NInspectCardScreen.");
            return;
        }
        if (inspectScreen.GetParent() is null)
            GetTree().Root.AddChild(inspectScreen);

        try
        {
            inspectScreen.Open(new List<CardModel> { card }, 0);
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

        foreach (var node in root.FindChildren("*", nameof(NInspectCardScreen), recursive: true, owned: false))
        {
            if (node is NInspectCardScreen screen) return screen;
        }
        return null;
    }
}

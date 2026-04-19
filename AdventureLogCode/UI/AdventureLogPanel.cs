using AdventureLog.AdventureLogCode.Events;
using AdventureLog.AdventureLogCode.UI.Rows;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace AdventureLog.AdventureLogCode.UI;

/// <summary>
/// Toggleable panel (F) showing events logged during the run.
/// Injected by UiInjectionPatch. Dispatches each LogEvent to its row type.
/// </summary>
public partial class AdventureLogPanel : Control
{
    private VBoxContainer _list = null!;
    private ScrollContainer _scroll = null!;
    private Label _status = null!;
    private CreatureHighlighter _highlighter = null!;
    private bool _isShown;
    private int _lastKnownCount;
    private bool _dragging;
    private Vector2 _dragStartMouse;
    private float _dragStartOffsetLeft;
    private float _dragStartOffsetRight;
    private float _dragStartOffsetTop;
    private float _dragStartOffsetBottom;

    private static AdventureLogPanel? _instance;
    public static AdventureLogPanel? Instance => _instance;

    public override void _Ready()
    {
        _instance = this;
        _highlighter = new CreatureHighlighter(this);

        var viewport = GetViewport().GetVisibleRect().Size;
        const float defaultWidth = 420f;
        const float defaultHeight = 260f;
        AnchorLeft = 0; AnchorRight = 0; AnchorTop = 0; AnchorBottom = 0;

        var saved = PanelSettings.Load();
        if (saved is not null)
        {
            OffsetLeft = saved.OffsetLeft;
            OffsetRight = saved.OffsetRight;
            OffsetTop = saved.OffsetTop;
            OffsetBottom = saved.OffsetBottom;
        }
        else
        {
            OffsetRight = viewport.X - 10;
            OffsetLeft = OffsetRight - defaultWidth;
            OffsetTop = viewport.Y * 0.07f + 15;
            OffsetBottom = OffsetTop + defaultHeight;
        }
        CustomMinimumSize = Vector2.Zero;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.Move;

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        styleBox.BorderColor = new Color(0.4f, 0.4f, 0.6f, 0.8f);
        styleBox.SetBorderWidthAll(2);
        styleBox.SetCornerRadiusAll(8);
        styleBox.SetContentMarginAll(10);

        var inner = new PanelContainer();
        inner.AnchorLeft = 0; inner.AnchorRight = 1;
        inner.AnchorTop = 0; inner.AnchorBottom = 1;
        inner.OffsetLeft = 0; inner.OffsetRight = 0;
        inner.OffsetTop = 0; inner.OffsetBottom = 0;
        inner.AddThemeStyleboxOverride("panel", styleBox);
        inner.MouseFilter = MouseFilterEnum.Pass;
        AddChild(inner);

        var vbox = new VBoxContainer();
        vbox.MouseFilter = MouseFilterEnum.Pass;
        inner.AddChild(vbox);

        _scroll = new ScrollContainer();
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.MouseFilter = MouseFilterEnum.Pass;
        vbox.AddChild(_scroll);

        _list = new VBoxContainer();
        _list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _list.MouseFilter = MouseFilterEnum.Pass;
        _scroll.AddChild(_list);

        vbox.AddChild(new HSeparator());

        _status = new Label();
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        _status.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_status);
        UpdateStatus();

        AddChild(new PanelEdgeHandle { Kind = PanelEdgeHandle.Edge.Left });
        AddChild(new PanelEdgeHandle { Kind = PanelEdgeHandle.Edge.Right });
        AddChild(new PanelEdgeHandle { Kind = PanelEdgeHandle.Edge.Top });
        AddChild(new PanelEdgeHandle { Kind = PanelEdgeHandle.Edge.Bottom });
        AddChild(new PanelEdgeHandle { Kind = PanelEdgeHandle.Edge.TopLeft });
        AddChild(new PanelEdgeHandle { Kind = PanelEdgeHandle.Edge.TopRight });
        AddChild(new PanelEdgeHandle { Kind = PanelEdgeHandle.Edge.BottomLeft });
        AddChild(new PanelEdgeHandle { Kind = PanelEdgeHandle.Edge.BottomRight });

        Visible = false;
        _isShown = false;

        AdventureLogTracker.OnHistoryChanged += OnHistoryChanged;
    }

    public override void _ExitTree()
    {
        AdventureLogTracker.OnHistoryChanged -= OnHistoryChanged;
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

    public override void _GuiInput(InputEvent ev)
    {
        switch (ev)
        {
            case InputEventMouseButton mb when mb.ButtonIndex == MouseButton.Left:
                if (mb.Pressed)
                {
                    _dragging = true;
                    _dragStartMouse = GetGlobalMousePosition();
                    _dragStartOffsetLeft = OffsetLeft;
                    _dragStartOffsetRight = OffsetRight;
                    _dragStartOffsetTop = OffsetTop;
                    _dragStartOffsetBottom = OffsetBottom;
                }
                else
                {
                    if (_dragging) SavePosition();
                    _dragging = false;
                }
                AcceptEvent();
                break;

            case InputEventMouseMotion when _dragging:
                var mouse = GetGlobalMousePosition();
                var dx = mouse.X - _dragStartMouse.X;
                var dy = mouse.Y - _dragStartMouse.Y;
                OffsetLeft = _dragStartOffsetLeft + dx;
                OffsetRight = _dragStartOffsetRight + dx;
                OffsetTop = _dragStartOffsetTop + dy;
                OffsetBottom = _dragStartOffsetBottom + dy;
                AcceptEvent();
                break;
        }
    }

    public void Toggle()
    {
        _isShown = !_isShown;
        Visible = _isShown;
        if (_isShown) RefreshList();
    }

    public void SavePosition() =>
        PanelSettings.Save(new PanelSettings.Data(OffsetLeft, OffsetRight, OffsetTop, OffsetBottom));

    private void OnHistoryChanged()
    {
        if (_isShown) RefreshList();
    }

    private void UpdateStatus()
    {
        var combat = AdventureLogTracker.CurrentCombat;
        _status.Text = combat > 0
            ? $"Combat {combat}  ·  F to toggle"
            : "F to toggle";
    }

    private void RefreshList()
    {
        UpdateStatus();
        var history = AdventureLogTracker.History;
        if (history.Count == _lastKnownCount) return;

        foreach (var child in _list.GetChildren())
            child.QueueFree();

        var items = CoalesceSourceGroups(BuildRenderItems(history));

        int lastTurn = -1;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];

            if (item.TurnNumber != lastTurn)
            {
                lastTurn = item.TurnNumber;
                _list.AddChild(BuildTurnHeader(item.TurnNumber));
            }

            AppendRowsFor(item);
        }

        _lastKnownCount = history.Count;
        CallDeferred(nameof(ScrollToTop));
    }

    private void AppendRowsFor(RenderItem item)
    {
        switch (item)
        {
            case CardRenderItem c:
                _list.AddChild(new CardEntryRow(c.Card, c.Damages, _highlighter, OpenInspectScreen));
                foreach (var g in GroupDamagesByVictim(c.Damages))
                    _list.AddChild(new DamageSubRow(
                        g.VictimName, g.VictimCombatId, c.Card.PlayerCombatId,
                        g.HpLost, g.Blocked, g.Killed, _highlighter));
                foreach (var p in c.Powers)
                    _list.AddChild(new PowerSubRow(p, _highlighter));
                break;
            case DamageRenderItem d:
                _list.AddChild(new DamageEntryRow(d.Damage, _highlighter));
                break;
            case RelicRenderItem r:
                _list.AddChild(new RelicEntryRow(r.Proc, _highlighter));
                var relicSource = r.Damages.FirstOrDefault()?.SourceCombatId;
                foreach (var g in GroupDamagesByVictim(r.Damages))
                    _list.AddChild(new DamageSubRow(
                        g.VictimName, g.VictimCombatId, relicSource,
                        g.HpLost, g.Blocked, g.Killed, _highlighter));
                foreach (var p in r.Powers)
                    _list.AddChild(new PowerSubRow(p, _highlighter));
                foreach (var e in r.EnergyDeltas)
                    _list.AddChild(new EnergySubRow(e, _highlighter));
                break;
            case PowerRenderItem p:
                _list.AddChild(new PowerEntryRow(p.Power, _highlighter));
                break;
            case EnergyRenderItem e:
                _list.AddChild(new EnergySubRow(e.Energy, _highlighter));
                break;
            case RecallRenderItem r:
                _list.AddChild(new CardRecallRow(r.Recall, OpenInspectScreen));
                break;
            case SourceGroupRenderItem g:
                _list.AddChild(new SourceHeaderRow(g.SourceName, g.SourceCombatId, _highlighter));
                foreach (var gd in GroupDamagesByVictim(g.Damages))
                    _list.AddChild(new DamageSubRow(
                        gd.VictimName, gd.VictimCombatId, g.SourceCombatId,
                        gd.HpLost, gd.Blocked, gd.Killed, _highlighter));
                foreach (var pe in g.Powers)
                    _list.AddChild(new PowerSubRow(pe, _highlighter));
                break;
        }
    }

    private readonly record struct VictimGroup(
        string VictimName, uint? VictimCombatId, int HpLost, int Blocked, bool Killed);

    private static List<VictimGroup> GroupDamagesByVictim(IReadOnlyList<DamageReceivedEvent> damages)
    {
        var result = new List<VictimGroup>();
        var indexByKey = new Dictionary<string, int>();
        foreach (var d in damages)
        {
            var key = d.VictimCombatId?.ToString() ?? $"name:{d.VictimName}";
            if (indexByKey.TryGetValue(key, out var idx))
            {
                var existing = result[idx];
                result[idx] = existing with
                {
                    HpLost = existing.HpLost + d.HpLost,
                    Blocked = existing.Blocked + d.BlockedDamage,
                    Killed = existing.Killed || d.WasKilled,
                };
            }
            else
            {
                indexByKey[key] = result.Count;
                result.Add(new VictimGroup(d.VictimName, d.VictimCombatId, d.HpLost, d.BlockedDamage, d.WasKilled));
            }
        }
        return result;
    }

    private abstract record RenderItem(int CombatNumber, int TurnNumber);
    private sealed record CardRenderItem(
        CardPlayEvent Card,
        IReadOnlyList<DamageReceivedEvent> Damages,
        IReadOnlyList<PowerReceivedEvent> Powers)
        : RenderItem(Card.CombatNumber, Card.TurnNumber);
    private sealed record DamageRenderItem(DamageReceivedEvent Damage)
        : RenderItem(Damage.CombatNumber, Damage.TurnNumber);
    private sealed record RelicRenderItem(
        RelicProcEvent Proc,
        IReadOnlyList<DamageReceivedEvent> Damages,
        IReadOnlyList<PowerReceivedEvent> Powers,
        IReadOnlyList<EnergyDeltaEvent> EnergyDeltas)
        : RenderItem(Proc.CombatNumber, Proc.TurnNumber);
    private sealed record PowerRenderItem(PowerReceivedEvent Power)
        : RenderItem(Power.CombatNumber, Power.TurnNumber);
    private sealed record EnergyRenderItem(EnergyDeltaEvent Energy)
        : RenderItem(Energy.CombatNumber, Energy.TurnNumber);
    private sealed record RecallRenderItem(CardRecallEvent Recall)
        : RenderItem(Recall.CombatNumber, Recall.TurnNumber);
    private sealed record SourceGroupRenderItem(
        string SourceName, uint? SourceCombatId,
        IReadOnlyList<DamageReceivedEvent> Damages,
        IReadOnlyList<PowerReceivedEvent> Powers,
        int CombatNumber, int TurnNumber)
        : RenderItem(CombatNumber, TurnNumber);

    private static (uint? id, string name)? SourceKey(RenderItem item) => item switch
    {
        DamageRenderItem d when d.Damage.SourceCombatId.HasValue && string.IsNullOrEmpty(d.Damage.SourceCardName)
            => (d.Damage.SourceCombatId, d.Damage.SourceName),
        PowerRenderItem p when p.Power.ApplierCombatId.HasValue
            => (p.Power.ApplierCombatId, p.Power.ApplierName ?? ""),
        _ => null,
    };

    private static List<RenderItem> CoalesceSourceGroups(List<RenderItem> items)
    {
        var result = new List<RenderItem>();
        int i = 0;
        while (i < items.Count)
        {
            var key = SourceKey(items[i]);
            if (key is null)
            {
                result.Add(items[i]);
                i++;
                continue;
            }

            int j = i + 1;
            while (j < items.Count)
            {
                var next = SourceKey(items[j]);
                if (next is null) break;
                if (next.Value.id != key.Value.id) break;
                if (items[j].TurnNumber != items[i].TurnNumber) break;
                if (items[j].CombatNumber != items[i].CombatNumber) break;
                j++;
            }

            int runLen = j - i;
            if (runLen < 2)
            {
                result.Add(items[i]);
                i++;
                continue;
            }

            var damages = new List<DamageReceivedEvent>();
            var powers = new List<PowerReceivedEvent>();
            for (int k = i; k < j; k++)
            {
                switch (items[k])
                {
                    case DamageRenderItem d: damages.Add(d.Damage); break;
                    case PowerRenderItem p: powers.Add(p.Power); break;
                }
            }
            result.Add(new SourceGroupRenderItem(
                key.Value.name, key.Value.id, damages, powers,
                items[i].CombatNumber, items[i].TurnNumber));
            i = j;
        }
        return result;
    }

    private static List<RenderItem> BuildRenderItems(IReadOnlyList<LogEvent> history)
    {
        var items = new List<RenderItem>();
        for (int i = 0; i < history.Count; i++)
        {
            switch (history[i])
            {
                case CardPlayEvent card:
                {
                    var damages = new List<DamageReceivedEvent>();
                    var powers = new List<PowerReceivedEvent>();
                    while (i + 1 < history.Count && TryConsumeCardChild(history[i + 1], card, damages, powers))
                        i++;
                    items.Add(new CardRenderItem(card, damages, powers));
                    break;
                }
                case DamageReceivedEvent damage:
                    items.Add(new DamageRenderItem(damage));
                    break;
                case RelicProcEvent relic:
                {
                    // Game emits damage before the relic flashes, so orphan damage rows
                    // (no card source, same turn) preceding the proc belong to this relic.
                    var damages = new List<DamageReceivedEvent>();
                    while (items.Count > 0
                           && items[^1] is DamageRenderItem tail
                           && tail.TurnNumber == relic.TurnNumber
                           && tail.CombatNumber == relic.CombatNumber
                           && string.IsNullOrEmpty(tail.Damage.SourceCardName))
                    {
                        damages.Insert(0, tail.Damage);
                        items.RemoveAt(items.Count - 1);
                    }
                    var powers = new List<PowerReceivedEvent>();
                    var energies = new List<EnergyDeltaEvent>();
                    while (i + 1 < history.Count
                           && history[i + 1].TurnNumber == relic.TurnNumber
                           && history[i + 1].CombatNumber == relic.CombatNumber
                           && (history[i + 1] is PowerReceivedEvent or EnergyDeltaEvent
                               || (history[i + 1] is DamageReceivedEvent dd
                                   && string.IsNullOrEmpty(dd.SourceCardName))))
                    {
                        switch (history[i + 1])
                        {
                            case DamageReceivedEvent d: damages.Add(d); break;
                            case PowerReceivedEvent p: powers.Add(p); break;
                            case EnergyDeltaEvent e: energies.Add(e); break;
                        }
                        i++;
                    }
                    items.Add(new RelicRenderItem(relic, damages, powers, energies));
                    break;
                }
                case PowerReceivedEvent power:
                    items.Add(new PowerRenderItem(power));
                    break;
                case EnergyDeltaEvent energy:
                    items.Add(new EnergyRenderItem(energy));
                    break;
                case CardRecallEvent recall:
                    items.Add(new RecallRenderItem(recall));
                    break;
            }
        }
        return items;
    }

    private static bool TryConsumeCardChild(
        LogEvent evt, CardPlayEvent card,
        List<DamageReceivedEvent> damages, List<PowerReceivedEvent> powers)
    {
        if (evt.TurnNumber != card.TurnNumber) return false;
        if (evt.CombatNumber != card.CombatNumber) return false;
        switch (evt)
        {
            case DamageReceivedEvent d
                when !string.IsNullOrEmpty(d.SourceCardName) && d.SourceCardName == card.CardName:
                damages.Add(d);
                return true;
            case PowerReceivedEvent p
                when p.ApplierCombatId.HasValue && p.ApplierCombatId == card.PlayerCombatId:
                powers.Add(p);
                return true;
            default:
                return false;
        }
    }

    private void ScrollToTop() => _scroll.ScrollVertical = 0;

    private static Label BuildTurnHeader(int turnNumber)
    {
        var label = new Label();
        label.Text = $"  Turn {turnNumber}:";
        label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.5f));
        return label;
    }


    private void OpenInspectScreen(CardModel card)
    {
        var inspectScreen = FindInspectCardScreen() ?? NInspectCardScreen.Create();
        if (inspectScreen is null)
        {
            GD.PrintErr("[AdventureLog] Failed to create NInspectCardScreen.");
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
            GD.PrintErr($"[AdventureLog] Failed to open inspect screen: {e.Message}");
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

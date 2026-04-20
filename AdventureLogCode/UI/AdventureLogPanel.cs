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
    private bool _dragging;
    private Vector2 _dragStartMouse;
    private float _dragStartOffsetLeft;
    private float _dragStartOffsetRight;
    private float _dragStartOffsetTop;
    private float _dragStartOffsetBottom;

    // Incremental render state. _items mirrors events ingested so far (oldest->newest).
    // _rowCountsPerItem[i] = number of rows _items[i] contributes to _list (excluding its turn header).
    // When _items[i] starts a new turn, a header row sits immediately above its rows in _list.
    // _currentTopTurn = turn number of the most recently pushed item (== turn of top header in _list).
    // _processedHistoryCount = next index in AdventureLogTracker.History to consume.
    private readonly List<RenderItem> _items = new();
    private readonly List<int> _rowCountsPerItem = new();
    private int _processedHistoryCount;
    private int _currentTopTurn = -1;

    // Sticky-top autoscroll: only jump to top on new events if user was already parked there.
    // Otherwise, preserve the viewport by shifting ScrollVertical by the layout delta caused
    // by inserting new rows above the visible area.
    private const int StickyTopThresholdPx = 8;

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
        if (_isShown) ProcessNewEvents();
    }

    private void UpdateStatus()
    {
        var combat = AdventureLogTracker.CurrentCombat;
        _status.Text = combat > 0
            ? $"Combat {combat}  ·  F to toggle"
            : "F to toggle";
    }

    // Full reset path. Used only on show/toggle — state is discarded and rebuilt from history.
    private void RefreshList()
    {
        UpdateStatus();
        foreach (var child in _list.GetChildren())
            child.QueueFree();
        _items.Clear();
        _rowCountsPerItem.Clear();
        _processedHistoryCount = 0;
        _currentTopTurn = -1;
        ProcessNewEvents();
        CallDeferred(nameof(ScrollToTop));
    }

    // Incremental path. Consumes any history entries past _processedHistoryCount without
    // touching sealed prior rows. O(new events), not O(|history|).
    private void ProcessNewEvents()
    {
        UpdateStatus();
        var history = AdventureLogTracker.History;

        // Tracker clears History at combat start. Detect via count-shrink and fall back to full reset.
        if (history.Count < _processedHistoryCount)
        {
            RefreshList();
            return;
        }

        if (_processedHistoryCount >= history.Count) return;

        bool wasPinnedToTop = _scroll.ScrollVertical <= StickyTopThresholdPx;
        double preHeight = MeasureListContentHeight();
        int preValue = _scroll.ScrollVertical;

        while (_processedHistoryCount < history.Count)
        {
            var evt = history[_processedHistoryCount];
            _processedHistoryCount++;
            IngestEvent(evt);
        }

        if (wasPinnedToTop)
        {
            CallDeferred(nameof(ScrollToTop));
            return;
        }

        // Rows insert at the top of _list, so the viewport drifts unless we compensate.
        // MaxValue won't update until the next layout pass, so sum min sizes ourselves
        // synchronously and shift ScrollVertical now — no one-frame jump.
        double newHeight = MeasureListContentHeight();
        double delta = newHeight - preHeight;
        if (delta > 0)
            _scroll.ScrollVertical = preValue + (int)delta;
    }

    private double MeasureListContentHeight()
    {
        var children = _list.GetChildren();
        if (children.Count == 0) return 0;
        double h = 0;
        foreach (var child in children)
        {
            if (child is Control c)
                h += c.GetCombinedMinimumSize().Y;
        }
        int separation = _list.GetThemeConstant("separation");
        h += separation * (children.Count - 1);
        return h;
    }

    private void IngestEvent(LogEvent evt)
    {
        // Open-tail absorption (CardRenderItem / PotionRenderItem / RelicRenderItem consume children).
        if (_items.Count > 0 && TryAbsorbIntoTail(_items[^1], evt, out var updated))
        {
            _items[^1] = updated;
            ReRenderTail();
            return;
        }

        // New top-level item.
        var newItem = CreateStandaloneItem(evt);
        if (newItem is null) return;

        // Relic back-absorption: orphan damage rows logged just before a relic flash belong to it.
        if (newItem is RelicRenderItem relic)
        {
            List<DamageReceivedEvent> absorbedDamages = [];
            while (_items.Count > 0
                   && _items[^1] is DamageRenderItem dr
                   && dr.TurnNumber == relic.TurnNumber
                   && dr.CombatNumber == relic.CombatNumber
                   && string.IsNullOrEmpty(dr.Damage.SourceCardName))
            {
                absorbedDamages.Insert(0, dr.Damage);
                PopTailItem();
            }
            if (absorbedDamages.Count > 0)
                newItem = relic with { Damages = absorbedDamages };
        }

        PushItem(newItem);
    }

    private bool TryAbsorbIntoTail(RenderItem tail, LogEvent evt, out RenderItem updated)
    {
        updated = tail;
        switch (tail)
        {
            case CardRenderItem c:
            {
                var damages = c.Damages.ToList();
                var powers = c.Powers.ToList();
                var recalls = c.Recalls.ToList();
                var energies = c.Energies.ToList();
                var draws = c.Draws.ToList();
                var discards = c.Discards.ToList();
                var exhausts = c.Exhausts.ToList();
                var blockGains = c.BlockGains.ToList();
                var upgrades = c.Upgrades.ToList();
                var generated = c.Generated.ToList();
                if (TryConsumeCardChild(evt, c.Card, damages, powers, recalls, energies,
                        draws, discards, exhausts, blockGains, upgrades, generated))
                {
                    updated = new CardRenderItem(c.Card, damages, powers, recalls, energies,
                        draws, discards, exhausts, blockGains, upgrades, generated);
                    return true;
                }
                return false;
            }
            case PotionRenderItem p:
            {
                var damages = p.Damages.ToList();
                var powers = p.Powers.ToList();
                var blocks = p.BlockGains.ToList();
                var energies = p.Energies.ToList();
                var upgrades = p.Upgrades.ToList();
                var generated = p.Generated.ToList();
                if (TryConsumePotionChild(evt, p.Potion, damages, powers, blocks, energies, upgrades, generated))
                {
                    updated = new PotionRenderItem(p.Potion, damages, powers, blocks, energies, upgrades, generated);
                    return true;
                }
                return false;
            }
            case RelicRenderItem r:
            {
                if (evt.TurnNumber != r.TurnNumber || evt.CombatNumber != r.CombatNumber) return false;
                switch (evt)
                {
                    case DamageReceivedEvent d when string.IsNullOrEmpty(d.SourceCardName):
                        updated = r with { Damages = r.Damages.Append(d).ToList() };
                        return true;
                    case PowerReceivedEvent pe:
                        updated = r with { Powers = r.Powers.Append(pe).ToList() };
                        return true;
                    case EnergyDeltaEvent ee:
                        updated = r with { EnergyDeltas = r.EnergyDeltas.Append(ee).ToList() };
                        return true;
                }
                return false;
            }
        }
        return false;
    }

    private static RenderItem? CreateStandaloneItem(LogEvent evt) => evt switch
    {
        CardPlayEvent card => new CardRenderItem(card, [], [], [], [], [], [], [], [], [], []),
        DamageReceivedEvent damage => new DamageRenderItem(damage),
        RelicProcEvent relic => new RelicRenderItem(relic, [], [], []),
        PowerReceivedEvent power => new PowerRenderItem(power),
        EnergyDeltaEvent energy => new EnergyRenderItem(energy),
        CardRecallEvent recall => new RecallRenderItem(recall),
        CardDrawEvent draw => new DrawRenderItem(draw),
        CardDiscardEvent discard => new DiscardRenderItem(discard),
        CardExhaustEvent exhaust => new ExhaustRenderItem(exhaust),
        CardAfflictionEvent affliction => new AfflictionRenderItem(affliction),
        BlockGainedEvent block => new BlockGainedRenderItem(block),
        PotionUsedEvent potion => new PotionRenderItem(potion, [], [], [], [], [], []),
        CardUpgradeEvent upgrade => new UpgradeRenderItem(upgrade),
        CardGeneratedEvent gen => new GeneratedRenderItem(gen),
        _ => null,
    };

    // Push a new top-level item at the TOP of _list (visually newest). Inserts a turn header
    // first if the item starts a new turn. Item rows always land at index 1 (below the top header).
    private void PushItem(RenderItem item)
    {
        if (item.TurnNumber != _currentTopTurn)
        {
            _currentTopTurn = item.TurnNumber;
            InsertNodeAt(BuildTurnHeader(item.TurnNumber), 0);
        }

        var rows = BuildRowsFor(item);
        for (int i = 0; i < rows.Count; i++)
            InsertNodeAt(rows[i], 1 + i);

        _items.Add(item);
        _rowCountsPerItem.Add(rows.Count);
    }

    // Rebuild only the tail item's rows in place. Tail rows are the N children directly below
    // the top turn header (which is always at index 0 when _items is non-empty).
    private void ReRenderTail()
    {
        int oldCount = _rowCountsPerItem[^1];
        for (int i = 0; i < oldCount; i++)
            RemoveChildAt(1);

        var rows = BuildRowsFor(_items[^1]);
        for (int i = 0; i < rows.Count; i++)
            InsertNodeAt(rows[i], 1 + i);

        _rowCountsPerItem[^1] = rows.Count;
    }

    // Remove the tail item (rows + its turn header if it was sole occupant of the top turn).
    private void PopTailItem()
    {
        int count = _rowCountsPerItem[^1];
        _rowCountsPerItem.RemoveAt(_rowCountsPerItem.Count - 1);
        _items.RemoveAt(_items.Count - 1);

        for (int i = 0; i < count; i++)
            RemoveChildAt(1);

        // If the popped item was the only one of the current top turn, drop the header too.
        if (_items.Count == 0 || _items[^1].TurnNumber != _currentTopTurn)
        {
            RemoveChildAt(0);
            _currentTopTurn = _items.Count > 0 ? _items[^1].TurnNumber : -1;
        }
    }

    private void InsertNodeAt(Node node, int index)
    {
        _list.AddChild(node);
        _list.MoveChild(node, index);
    }

    private void RemoveChildAt(int index)
    {
        var child = _list.GetChild(index);
        _list.RemoveChild(child);
        child.QueueFree();
    }

    private List<Node> BuildRowsFor(RenderItem item)
    {
        List<Node> rows = [];
        switch (item)
        {
            case CardRenderItem c:
                rows.Add(new CardEntryRow(c.Card, c.Damages, _highlighter, OpenInspectScreen));
                foreach (var g in GroupDamagesByVictim(c.Damages))
                    rows.Add(new DamageSubRow(
                        g.VictimName, g.VictimCombatId, c.Card.PlayerCombatId,
                        g.HpLost, g.Blocked, g.Killed, g.Modifiers, _highlighter));
                foreach (var p in c.Powers)
                    rows.Add(new PowerSubRow(p, _highlighter));
                foreach (var e in c.Energies)
                    rows.Add(new EnergySubRow(e, _highlighter, showSource: false));
                foreach (var r in c.Recalls)
                    rows.Add(new CardRecallRow(r, OpenInspectScreen));
                foreach (var d in c.Draws)
                    rows.Add(new CardDrawRow(d, OpenInspectScreen));
                foreach (var dc in c.Discards)
                    rows.Add(new CardDiscardRow(dc, OpenInspectScreen));
                foreach (var b in c.BlockGains)
                    rows.Add(new BlockGainedRow(b, _highlighter, showSource: false));
                foreach (var u in c.Upgrades)
                    rows.Add(new CardUpgradeRow(u, OpenInspectScreen));
                foreach (var gen in c.Generated)
                    rows.Add(new CardGeneratedRow(gen, OpenInspectScreen));
                foreach (var ex in c.Exhausts)
                    rows.Add(new CardExhaustRow(ex, OpenInspectScreen));
                break;
            case DamageRenderItem d:
                rows.Add(new DamageEntryRow(d.Damage, _highlighter));
                break;
            case RelicRenderItem r:
                rows.Add(new RelicEntryRow(r.Proc, _highlighter));
                var relicSource = r.Damages.FirstOrDefault()?.SourceCombatId;
                foreach (var g in GroupDamagesByVictim(r.Damages))
                    rows.Add(new DamageSubRow(
                        g.VictimName, g.VictimCombatId, relicSource,
                        g.HpLost, g.Blocked, g.Killed, g.Modifiers, _highlighter));
                foreach (var p in r.Powers)
                    rows.Add(new PowerSubRow(p, _highlighter));
                foreach (var e in r.EnergyDeltas)
                    rows.Add(new EnergySubRow(e, _highlighter));
                break;
            case PowerRenderItem p:
                rows.Add(new PowerEntryRow(p.Power, _highlighter));
                break;
            case EnergyRenderItem e:
                rows.Add(new EnergySubRow(e.Energy, _highlighter));
                break;
            case RecallRenderItem r:
                rows.Add(new CardRecallRow(r.Recall, OpenInspectScreen));
                break;
            case DrawRenderItem d:
                rows.Add(new CardDrawRow(d.Draw, OpenInspectScreen));
                break;
            case DiscardRenderItem d:
                rows.Add(new CardDiscardRow(d.Discard, OpenInspectScreen));
                break;
            case ExhaustRenderItem ex:
                rows.Add(new CardExhaustRow(ex.Exhaust, OpenInspectScreen));
                break;
            case AfflictionRenderItem a:
                rows.Add(new CardAfflictionRow(a.Affliction, OpenInspectScreen));
                break;
            case BlockGainedRenderItem b:
                rows.Add(new BlockGainedRow(b.Block, _highlighter));
                break;
            case PotionRenderItem p:
                rows.Add(new PotionUsedRow(p.Potion, _highlighter));
                foreach (var gd in GroupDamagesByVictim(p.Damages))
                    rows.Add(new DamageSubRow(
                        gd.VictimName, gd.VictimCombatId, null,
                        gd.HpLost, gd.Blocked, gd.Killed, gd.Modifiers, _highlighter));
                foreach (var pw in p.Powers)
                    rows.Add(new PowerSubRow(pw, _highlighter));
                foreach (var b in p.BlockGains)
                    rows.Add(new BlockGainedRow(b, _highlighter, showSource: false));
                foreach (var e in p.Energies)
                    rows.Add(new EnergySubRow(e, _highlighter, showSource: false));
                foreach (var u in p.Upgrades)
                    rows.Add(new CardUpgradeRow(u, OpenInspectScreen));
                foreach (var gen in p.Generated)
                    rows.Add(new CardGeneratedRow(gen, OpenInspectScreen));
                break;
            case UpgradeRenderItem u:
                rows.Add(new CardUpgradeRow(u.Upgrade, OpenInspectScreen));
                break;
            case GeneratedRenderItem g:
                rows.Add(new CardGeneratedRow(g.Generated, OpenInspectScreen));
                break;
        }
        return rows;
    }

    private sealed record VictimGroup(
        string VictimName, uint? VictimCombatId, int HpLost, int Blocked, bool Killed,
        IReadOnlyList<string> Modifiers);

    private static List<VictimGroup> GroupDamagesByVictim(IReadOnlyList<DamageReceivedEvent> damages)
    {
        List<VictimGroup> result = [];
        Dictionary<string, int> indexByKey = [];
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
                    Modifiers = MergeModifiers(existing.Modifiers, d.Modifiers),
                };
            }
            else
            {
                indexByKey[key] = result.Count;
                result.Add(new VictimGroup(
                    d.VictimName, d.VictimCombatId, d.HpLost, d.BlockedDamage, d.WasKilled,
                    d.Modifiers));
            }
        }
        return result;
    }

    private static IReadOnlyList<string> MergeModifiers(
        IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (b.Count == 0) return a;
        if (a.Count == 0) return b;
        List<string> merged = [.. a];
        HashSet<string> seen = new(a, StringComparer.Ordinal);
        foreach (var name in b)
            if (seen.Add(name)) merged.Add(name);
        return merged;
    }

    private abstract record RenderItem(int CombatNumber, int TurnNumber);
    private sealed record CardRenderItem(
        CardPlayEvent Card,
        IReadOnlyList<DamageReceivedEvent> Damages,
        IReadOnlyList<PowerReceivedEvent> Powers,
        IReadOnlyList<CardRecallEvent> Recalls,
        IReadOnlyList<EnergyDeltaEvent> Energies,
        IReadOnlyList<CardDrawEvent> Draws,
        IReadOnlyList<CardDiscardEvent> Discards,
        IReadOnlyList<CardExhaustEvent> Exhausts,
        IReadOnlyList<BlockGainedEvent> BlockGains,
        IReadOnlyList<CardUpgradeEvent> Upgrades,
        IReadOnlyList<CardGeneratedEvent> Generated)
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
    private sealed record DrawRenderItem(CardDrawEvent Draw)
        : RenderItem(Draw.CombatNumber, Draw.TurnNumber);
    private sealed record DiscardRenderItem(CardDiscardEvent Discard)
        : RenderItem(Discard.CombatNumber, Discard.TurnNumber);
    private sealed record ExhaustRenderItem(CardExhaustEvent Exhaust)
        : RenderItem(Exhaust.CombatNumber, Exhaust.TurnNumber);
    private sealed record AfflictionRenderItem(CardAfflictionEvent Affliction)
        : RenderItem(Affliction.CombatNumber, Affliction.TurnNumber);
    private sealed record BlockGainedRenderItem(BlockGainedEvent Block)
        : RenderItem(Block.CombatNumber, Block.TurnNumber);
    private sealed record PotionRenderItem(
        PotionUsedEvent Potion,
        IReadOnlyList<DamageReceivedEvent> Damages,
        IReadOnlyList<PowerReceivedEvent> Powers,
        IReadOnlyList<BlockGainedEvent> BlockGains,
        IReadOnlyList<EnergyDeltaEvent> Energies,
        IReadOnlyList<CardUpgradeEvent> Upgrades,
        IReadOnlyList<CardGeneratedEvent> Generated)
        : RenderItem(Potion.CombatNumber, Potion.TurnNumber);
    private sealed record UpgradeRenderItem(CardUpgradeEvent Upgrade)
        : RenderItem(Upgrade.CombatNumber, Upgrade.TurnNumber);
    private sealed record GeneratedRenderItem(CardGeneratedEvent Generated)
        : RenderItem(Generated.CombatNumber, Generated.TurnNumber);

    private static bool TryConsumeCardChild(
        LogEvent evt, CardPlayEvent card,
        List<DamageReceivedEvent> damages, List<PowerReceivedEvent> powers,
        List<CardRecallEvent> recalls, List<EnergyDeltaEvent> energies,
        List<CardDrawEvent> draws, List<CardDiscardEvent> discards,
        List<CardExhaustEvent> exhausts, List<BlockGainedEvent> blockGains,
        List<CardUpgradeEvent> upgrades, List<CardGeneratedEvent> generated)
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
                when !string.IsNullOrEmpty(p.SourceCardName) && p.SourceCardName == card.CardName
                     && ((p.ApplierCombatId.HasValue && p.ApplierCombatId == card.PlayerCombatId)
                         || (p.OwnerCreatureCombatId.HasValue && p.OwnerCreatureCombatId == card.PlayerCombatId)):
                powers.Add(p);
                return true;
            case EnergyDeltaEvent e
                when e.PlayerCombatId.HasValue && e.PlayerCombatId == card.PlayerCombatId:
                energies.Add(e);
                return true;
            case CardRecallEvent r when r.OwnerNetId == card.OwnerNetId:
                recalls.Add(r);
                return true;
            case CardDrawEvent d when d.OwnerNetId == card.OwnerNetId:
                draws.Add(d);
                return true;
            case CardDiscardEvent dc when dc.OwnerNetId == card.OwnerNetId:
                discards.Add(dc);
                return true;
            case CardExhaustEvent ex when ex.OwnerNetId == card.OwnerNetId:
                exhausts.Add(ex);
                return true;
            case BlockGainedEvent b
                when !string.IsNullOrEmpty(b.SourceCardName) && b.SourceCardName == card.CardName
                     && b.OwnerNetId == card.OwnerNetId:
                blockGains.Add(b);
                return true;
            case CardUpgradeEvent u when u.OwnerNetId == card.OwnerNetId:
                upgrades.Add(u);
                return true;
            case CardGeneratedEvent g when g.GeneratedByPlayer && g.OwnerNetId == card.OwnerNetId:
                generated.Add(g);
                return true;
            default:
                return false;
        }
    }

    // Potion-driven effects don't carry a SourceCardName (potions don't pass cardSource
    // through Power/Block/Damage commands). Match by null-source + same owner so the
    // immediate effects after OnUseWrapper nest under the potion row.
    private static bool TryConsumePotionChild(
        LogEvent evt, PotionUsedEvent potion,
        List<DamageReceivedEvent> damages, List<PowerReceivedEvent> powers,
        List<BlockGainedEvent> blockGains, List<EnergyDeltaEvent> energies,
        List<CardUpgradeEvent> upgrades, List<CardGeneratedEvent> generated)
    {
        if (evt.TurnNumber != potion.TurnNumber) return false;
        if (evt.CombatNumber != potion.CombatNumber) return false;
        switch (evt)
        {
            case PowerReceivedEvent p
                when string.IsNullOrEmpty(p.SourceCardName) && p.OwnerNetId == potion.OwnerNetId:
                powers.Add(p);
                return true;
            case BlockGainedEvent b
                when string.IsNullOrEmpty(b.SourceCardName) && b.OwnerNetId == potion.OwnerNetId:
                blockGains.Add(b);
                return true;
            case DamageReceivedEvent d
                when string.IsNullOrEmpty(d.SourceCardName) && d.OwnerNetId == potion.OwnerNetId:
                damages.Add(d);
                return true;
            case EnergyDeltaEvent e
                when string.IsNullOrEmpty(e.SourceCardName) && e.OwnerNetId == potion.OwnerNetId:
                energies.Add(e);
                return true;
            case CardUpgradeEvent u when u.OwnerNetId == potion.OwnerNetId:
                upgrades.Add(u);
                return true;
            case CardGeneratedEvent g when g.GeneratedByPlayer && g.OwnerNetId == potion.OwnerNetId:
                generated.Add(g);
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
            inspectScreen.Open([card], 0);
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

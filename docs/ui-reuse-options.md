# Reusable StS2 UIs for Adventure Log Panel

Inventory of game-native list/row UI classes we could reuse in
`AdventureLogCode/UI/AdventureLogPanel.cs`. Decompiled from the publicized DLL at
`.godot/mono/temp/obj/Debug/PublicizedAssemblies/sts2.*/sts2.dll` (run
`Scripts/decompile-sts2.sh` to refresh the `.decompiled/` cache).

## POC status

| Row         | Attempted swap                                            | Status |
|-------------|-----------------------------------------------------------|--------|
| DamageSubRow| → `NStatEntry` (`screens/stats_screen/stats_screen_section`) | in progress |
| CardEntryRow| → `NDeckHistoryEntry`                                     | deferred |
| RelicEntryRow| → `NStatEntry`                                           | deferred |
| PowerSubRow | → `NStatEntry`                                            | deferred |
| scroll      | → `NScrollableContainer`                                  | **blocked**, see notes |

Update this table as swaps land or get rejected.

---

## NStatEntry — primary candidate (two-line text + icon)
`MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen.NStatEntry` (inherits
`NClickableControl`).

- Scene: `res://scenes/screens/stats_screen/stats_screen_section.tscn`
  (resolved via `SceneHelper.GetScenePath("screens/stats_screen/stats_screen_section")`)
- Factory: `NStatEntry.Create(string imgUrl)` — instantiates the scene and
  stores `imgUrl`. `_Ready()` then calls `PreloadManager.Cache.GetTexture2D(_imgUrl)`.
- Setters (call AFTER `_Ready`): `SetTopText(string)`, `SetBottomText(string)`,
  `SetHoverTip(HoverTip)`.
- Layout: `TextureRect _icon` (left) + `MegaRichTextLabel _topLabel` +
  `MegaRichTextLabel _bottomLabel` (right, stacked). `MegaRichTextLabel`
  supports BBCode — use `[color=#RRGGBB]...[/color]` for mixed-color lines.
- Free-with-subclass: focus tween (1.05× on focus), controller selection
  reticle, optional `NHoverTipSet` tooltip on focus (if far from viewport edge).

Usage (mod):
```csharp
var entry = PreloadManager.Cache.GetScene(NStatEntry.ScenePath)
    .Instantiate<NStatEntry>();
parent.AddChild(entry);
entry.Ready += () => {
    entry._icon.Visible = false;        // if no icon desired
    entry.SetTopText("top");
    entry.SetBottomText("bottom");
};
```

If `_imgUrl` is left null, `_Ready()` skips the texture load (the
`if (_imgUrl != null)` guard). Hide `_icon` in the `Ready` callback to
collapse the empty TextureRect slot.

## NDeckHistoryEntry — card rows
`MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen.NDeckHistoryEntry`
(inherits `NButton`).

- Scene: `res://scenes/screens/run_history_screen/deck_history_entry.tscn`
- No public `Create` factory — instantiate + set `Card`, `_amount`, then
  call `Reload()` (or wait for `_Ready` which calls `Reload` itself).
- Layout: `NTinyCard _cardImage` (left) + `MegaLabel _titleLabel` (amount
  prefix, e.g. "2x Bash") + `TextureRect _enchantmentImage` overlay.
- Fit for `CardEntryRow`: excellent.

```csharp
var entry = PreloadManager.Cache.GetScene(NDeckHistoryEntry.ScenePath)
    .Instantiate<NDeckHistoryEntry>();
entry.Card = cardModel;
entry._amount = 1;
entry.FloorsAddedToDeck = Array.Empty<int>();
parent.AddChild(entry);  // _Ready will call Reload()
```

## NScrollableContainer — BLOCKED for mod use
`MegaCrit.Sts2.Core.Nodes.GodotExtensions.NScrollableContainer`

- **No public scene path, no standalone `.tscn`.** Each game screen
  ships its own scene that contains an `NScrollableContainer` with
  pre-wired `Content` and `Scrollbar` children.
- `_Ready()` requires `GetNode<NScrollbar>("Scrollbar")` and a `Content`
  (or `Mask/Content`) child — `new NScrollableContainer()` from code will
  crash immediately.
- Scroll reset method: `InstantlyScrollToTop()` (not `ScrollVertical = 0`).
- **Path forward if we want this:** author our own `.tscn` under mod
  resources that pre-wires the required children, and `LookupScriptsInAssembly`
  to register it. Not worth the work unless row swaps succeed first.

## NHoverTipSet — keyword / card tooltips
`NHoverTipSet.CreateAndShow(Control owner, IEnumerable<IHoverTip> tips, HoverTipAlignment alignment)`

- Scene: `res://scenes/ui/hover_tip_set.tscn`
- `IHoverTip` implementors: `Id`, `IsSmart`, `IsDebuff`, `IsInstanced`,
  `CanonicalModel`.
- Auto-deduplicates by `Id`, auto-cleans on owner tree exit.
- Orthogonal to our `CreatureHighlighter` — additive, not a replacement.

## NActHistoryEntry — section headers (deferred)
Used in run-history screen as "Act 1 / Act 2" headers above sub-entries.
Could replace our `--- Combat N ---` and `Turn N:` labels for game-native
header typography. Defer until row-swap POC succeeds.

## Observations

- **No shared `NListContainer` / `NEntryList` utility.** Each screen rolls
  its own `VBoxContainer` + row instantiation loop.
- Rows inherit `NClickableControl` or `NButton` → focus/hover tweens and
  controller support for free.
- Styling pattern: theme color overrides (`AddThemeColorOverride("font_color", ...)`),
  never per-row `StyleBoxFlat`. Scene `.tscn` files bake in the default theme.
- Instantiation pattern: `PreloadManager.Cache.GetScene(scenePath).Instantiate<T>()`
  or (sugar) `SceneHelper.Instantiate<T>(innerPath)`.
- Setters on entry scenes (e.g. `SetTopText`) dereference fields that are
  populated in `_Ready()` — **always call after `AddChild` or via the
  `Ready` signal**, not before.

using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace AdventureLog.AdventureLogCode.UI;

public partial class HistoryButton : TextureRect
{
    private const string TexturePath = "res://images/relics/history_course.png";
    private const float HoverScale = 1.15f;
    private const float TweenDuration = 0.1f;

    private Tween? _tween;
    private static IHoverTip? _cachedHoverTip;

    public override void _Ready()
    {
        var tex = GD.Load<Texture2D>(TexturePath);
        if (tex is null)
        {
            GD.PrintErr($"[AdventureLog] HistoryButton: texture not found at '{TexturePath}'.");
            return;
        }

        Texture = tex;
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        MouseFilter = MouseFilterEnum.Stop;
        SelfModulate = new Color(1.6f, 1.6f, 1.6f, 1);

        PivotOffset = Size / 2;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        GuiInput += OnGuiInput;
    }

    private static IHoverTip CreateHoverTip(string title, string description)
    {
        object box = new HoverTip();
        var type = typeof(HoverTip);
        type.GetProperty("Title")!.SetValue(box, title);
        type.GetProperty("Description")!.SetValue(box, description);
        type.GetProperty("Id")!.SetValue(box, "AdventureLog_HistoryButton");
        return (IHoverTip)box;
    }

    private NDiscardPileButton? FindDiscardButton()
    {
        return GetParent()
            ?.FindChildren("*", recursive: false, owned: false)
            .OfType<NDiscardPileButton>()
            .FirstOrDefault();
    }

    private void OnMouseEntered()
    {
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(this, "scale", new Vector2(HoverScale, HoverScale), TweenDuration)
            .SetEase(Tween.EaseType.Out);

        _cachedHoverTip ??= CreateHoverTip(
            "Adventure Log (F)",
            "Tracks all cards played during the run.\n\nClick to toggle the combat log.");

        var tipSet = NHoverTipSet.CreateAndShow(this, _cachedHoverTip);
        if (tipSet is null) return;

        // Same spot the game uses for the discard pile tip (NCombatCardPile.OnFocus)
        var anchor = FindDiscardButton()?.GlobalPosition ?? GlobalPosition;
        tipSet.GlobalPosition = anchor + new Vector2(-320f, -370f);
    }

    private void OnMouseExited()
    {
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(this, "scale", Vector2.One, TweenDuration)
            .SetEase(Tween.EaseType.Out);

        NHoverTipSet.Remove(this);
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            AdventureLogPanel.Instance?.Toggle();
        }
    }
}

using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CombatLog.CombatLogCode.UI;

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
            GD.PrintErr($"[CombatLog] HistoryButton: texture not found at '{TexturePath}'.");
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
        type.GetProperty("Id")!.SetValue(box, "CombatLog_HistoryButton");
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

        var discardBtn = FindDiscardButton();
        if (discardBtn is null) return;

        var trav = Traverse.Create(discardBtn);

        // Swap the discard button's hover tip with ours
        var originalTip = trav.Field("_hoverTip").GetValue();
        _cachedHoverTip ??= CreateHoverTip(
            "Combat Log (F)",
            "Tracks all cards played during the run.\n\nClick to toggle the combat log.");
        trav.Field("_hoverTip").SetValue(_cachedHoverTip);

        // Show via game's code path for perfect positioning
        trav.Method("OnFocus").GetValue();

        // Suppress discard button's scale animation
        trav.Field<Tween>("_bumpTween").Value?.Kill();
        discardBtn.Scale = Vector2.One;

        // Restore original hover tip so discard pile still works
        trav.Field("_hoverTip").SetValue(originalTip);
    }

    private void OnMouseExited()
    {
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(this, "scale", Vector2.One, TweenDuration)
            .SetEase(Tween.EaseType.Out);

        var discardBtn = FindDiscardButton();
        if (discardBtn is null) return;

        var trav = Traverse.Create(discardBtn);
        trav.Method("OnUnfocus").GetValue();
        trav.Field<Tween>("_bumpTween").Value?.Kill();
        discardBtn.Scale = Vector2.One;
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            CombatLogPanel.Instance?.Toggle();
        }
    }
}

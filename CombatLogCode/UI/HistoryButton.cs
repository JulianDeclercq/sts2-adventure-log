using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CombatLog.CombatLogCode.UI;

public partial class HistoryButton : TextureRect
{
    private const string TexturePath = "res://images/relics/history_course.png";
    private const float HoverScale = 1.15f;
    private const float TweenDuration = 0.1f;

    private Tween? _tween;

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

    private NDiscardPileButton? FindDiscardButton()
    {
        return GetParent()
            ?.FindChildren("*", recursive: false, owned: false)
            .OfType<NDiscardPileButton>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Trigger the discard button's native OnFocus to show the hover tip at the
    /// exact same position, then suppress the visual side-effects (scale bump).
    /// </summary>
    private void OnMouseEntered()
    {
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(this, "scale", new Vector2(HoverScale, HoverScale), TweenDuration)
            .SetEase(Tween.EaseType.Out);

        var discardBtn = FindDiscardButton();
        if (discardBtn is null) return;

        // Show hover tip via the game's own code path
        Traverse.Create(discardBtn).Method("OnFocus").GetValue();

        // Kill the discard button's scale animation and reset
        Traverse.Create(discardBtn).Field<Tween>("_bumpTween").Value?.Kill();
        discardBtn.Scale = Vector2.One;
    }

    private void OnMouseExited()
    {
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(this, "scale", Vector2.One, TweenDuration)
            .SetEase(Tween.EaseType.Out);

        var discardBtn = FindDiscardButton();
        if (discardBtn is null) return;

        Traverse.Create(discardBtn).Method("OnUnfocus").GetValue();
        Traverse.Create(discardBtn).Field<Tween>("_bumpTween").Value?.Kill();
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
